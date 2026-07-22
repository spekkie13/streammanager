using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.ClashOfClans.Ccn;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Events;
using SpekkieClassLibrary.Overlay;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Overlay;

public class OverlayStateService(
    WarService warService,
    OverlayModeController modeController,
    OverlayLayoutController layoutController,
    SpotlightSelectionReader selectionReader,
    CcnHttpClient ccn,
    ITwitchChannelInfoClient twitchApi,
    ITwitchAuthTokenProvider tokens,
    ISpotifyService spotify,
    IStreamEventBus eventBus,
    TimerFileReader timerReader,
    SupportTotalsFeature supportTotals,
    ITwitchFileReader twitchFiles,
    Logger logger)
    : BackgroundService, IOverlayAfkWriter, IOverlayTimerWriter
{
    private static readonly string OutputPath =
        Path.Combine(BotPaths.BaseDir, "Output", "overlay-state.json");

    private static readonly string ActivePlayerPath =
        Path.Combine(BotPaths.BaseDir, "Output", "ClashOfClans", "active-player.json");

    // Same shape as the other overlay files, but omit null optionals so an inactive spotlight is just
    // { "active": false, "updatedAt": ... } rather than a wall of empty fields.
    private static readonly JsonSerializerOptions ActivePlayerOptions =
        new(OverlayJson.Options) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly string ConfigPath =
        Path.Combine(BotPaths.BaseDir, "Settings", "overlay-event.json");

    // Serializes overlay-state.json writes so an on-demand !afk/!back write can't collide with the
    // 5-second loop write (both go through WriteOverlayStateAsync).
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // Set by !afk/!back via SetAfkAsync (command thread), read by the writer loop. Volatile so the
    // loop sees the latest value without a lock.
    private volatile bool _afk;

    // Set by !pausetimer/!starttimer via SetTimerRunningAsync (command thread), read by the writer loop.
    // Volatile so the loop sees the latest value without a lock. The marathon timer starts paused.
    private volatile bool _timerRunning;

    private OverlayEventConfig _eventConfig = new();
    // Last successfully read goals config, reused if a tick reads goals.json mid-write (partial JSON).
    private StreamGoalsConfig? _lastGoals;
    private string _accountName = "";
    private string _latestFollower = "";
    private int _followerCount;
    private string _latestSub = "";
    private int _subscriberCount;
    private FileSystemWatcher? _configWatcher;
    private CancellationTokenSource? _watcherDebounce;
    private CancellationToken _stopToken;

    // CCN career stats keyed by player tag. Cached for the current war (cleared when the war changes)
    // so we hit CCN at most once per player per war rather than every tick. Null = CCN had no data.
    private readonly Dictionary<string, CcnPlayerInfo?> _careerCache = new();
    private string? _careerCacheWarId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stopToken = stoppingToken;

        await InitializeSupportDataAsync(stoppingToken);
        LoadEventConfig();
        StartConfigWatcher();

        eventBus.Subscribe<FollowHappened>(OnFollowAsync);
        eventBus.Subscribe<SubHappened>(OnSubAsync);

        while (!stoppingToken.IsCancellationRequested)
        {
            await WriteOverlayStateAsync(stoppingToken);
            await WriteActivePlayerAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task InitializeSupportDataAsync(CancellationToken ct)
    {
        try
        {
            _accountName = (await tokens.ReadIdentityAsync(ct)).BroadcasterName ?? "";
            _followerCount = await twitchApi.GetFollowerCount(ct);
            _latestFollower = await twitchApi.GetLatestFollower(ct);
            _subscriberCount = await twitchApi.GetSubscriberCount(ct);
            _latestSub = await twitchApi.GetLatestSubscriber(ct);
        }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Failed to initialize support data: {ex.Message}");
        }
    }

    private Task OnFollowAsync(FollowHappened e, CancellationToken _)
    {
        _latestFollower = e.UserName;
        _followerCount++;
        return Task.CompletedTask;
    }

    private Task OnSubAsync(SubHappened e, CancellationToken _)
    {
        // CommunityGift fires once for the gift bomb; individual Gift events follow per recipient
        if (e.Kind == SubKind.CommunityGift) return Task.CompletedTask;
        _latestSub = e.RecipientUserName;
        _subscriberCount++;
        return Task.CompletedTask;
    }

    // Flip the afk flag and persist immediately so !afk/!back take effect on the overlay without
    // waiting for the next loop tick. The flag itself is also read by the loop write.
    public async Task SetAfkAsync(bool afk, bool timerRunning, CancellationToken ct)
    {
        _afk = afk;
        _timerRunning = timerRunning;
        await WriteOverlayStateAsync(ct);
    }

    // Flip the timerRunning flag and persist immediately so !pausetimer/!starttimer take effect on the
    // overlay without waiting for the next loop tick. The flag itself is also read by the loop write.
    public async Task SetTimerRunningAsync(bool running, CancellationToken ct)
    {
        _timerRunning = running;
        await WriteOverlayStateAsync(ct);
    }

    private async Task WriteOverlayStateAsync(CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            (string title, string artist) = await GetNowPlayingAsync(ct);
            RunTimeWar? war = warService.LastKnownWar;

            string timer = timerReader.ReadRemainingTime();
            SupportTotals totals = supportTotals.Snapshot;
            OverlaySubGoal subGoal = await ReadSubGoalAsync();

            OverlayState state = new()
            {
                Mode = modeController.Resolve(),
                Layout = layoutController.Resolve(),
                UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Timer = timer,
                IsWarActive = warService.IsWarActive,
                Afk = _afk,
                TimerRunning = _timerRunning,
                AccountName = _accountName,
                Event = new OverlayEventInfo
                {
                    Title = _eventConfig.Title,
                    Subtitle = _eventConfig.Subtitle
                },
                War = war != null ? BuildWarOverlay(war) : new OverlayWar(),
                Support = new OverlaySupport
                {
                    LatestFollower = _latestFollower,
                    FollowerCount = _followerCount,
                    LatestSub = _latestSub,
                    SubscriberCount = _subscriberCount,
                    BitsTotal = totals.BitsTotal,
                    DonationTotal = totals.DonationTotal,
                    SubGoal = subGoal,
                    NowPlaying = new OverlayNowPlaying { Title = title, Artist = artist },
                    Socials = _eventConfig.Socials
                },
                Info = _eventConfig.Info
            };

            string json = JsonSerializer.Serialize(state, OverlayJson.Options);
            await OverlayJson.WriteAtomicAsync(OutputPath, json, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Error writing overlay state: {ex.Message}");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    // Live Attack Spotlight: resolve the StreamDeck selection against the current in-memory war and
    // publish a self-contained active-player.json. The current war is the source of truth — we never
    // read war-roster.json back. Any of: no selection, no active war, or a missing position yields an
    // inactive payload. Never throws into the loop.
    private async Task WriteActivePlayerAsync(CancellationToken ct)
    {
        try
        {
            ActivePlayer activePlayer = await BuildActivePlayerAsync(ct);
            string json = JsonSerializer.Serialize(activePlayer, ActivePlayerOptions);
            await OverlayJson.WriteAtomicAsync(ActivePlayerPath, json, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Error writing active player: {ex.Message}");
        }
    }

    private async Task<ActivePlayer> BuildActivePlayerAsync(CancellationToken ct)
    {
        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        SpotlightSelection? selection = selectionReader.Read();
        RunTimeWar? war = warService.LastKnownWar;

        ActivePlayer player = SpotlightMapper.BuildActivePlayer(
            war, warService.IsWarActive, selection?.Team, selection?.Position, now);

        // Enrich with the player's CCN career stats (offense/defense), keyed by their tag.
        if (player is { Active: true, Tag: { Length: > 0 } tag })
        {
            CcnPlayerInfo? info = await GetCareerAsync(tag, player.WarId ?? "", ct);
            SpotlightCareer? career = SpotlightMapper.BuildCareer(info);
            if (career != null)
                player = SpotlightMapper.BuildActivePlayer(
                    war, warService.IsWarActive, selection?.Team, selection?.Position, now, career);
        }

        return player;
    }

    private async Task<CcnPlayerInfo?> GetCareerAsync(string tag, string warId, CancellationToken ct)
    {
        if (_careerCacheWarId != warId)
        {
            _careerCache.Clear();
            _careerCacheWarId = warId;
        }

        if (_careerCache.TryGetValue(tag, out CcnPlayerInfo? cached))
            return cached;

        CcnPlayerInfo? info = await ccn.GetPlayerInfoAsync(tag, ct);
        _careerCache[tag] = info;
        return info;
    }

    // Read the active sub goal from goals.json (kept current by FollowSubFeature). The displayed goal
    // is the next unreached tier; once all tiers are cleared Goal collapses to the count and Reward is
    // empty. A failed/partial read falls back to the last good config so the overlay never flickers.
    private async Task<OverlaySubGoal> ReadSubGoalAsync()
    {
        try
        {
            StreamGoalsConfig? goals = await twitchFiles.ReadGoalsConfigAsync();
            if (goals != null) _lastGoals = goals;
        }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Failed to read goals config: {ex.Message}");
        }

        SubGoalConfig? sub = _lastGoals?.SubGoal;
        if (sub == null) return new OverlaySubGoal();

        SubGoalTier? next = sub.NextTier;
        return new OverlaySubGoal
        {
            Current = sub.CurrentAmount,
            Goal = next?.Goal ?? sub.CurrentAmount,
            Reward = next?.RewardEn ?? ""
        };
    }

    private async Task<(string title, string artist)> GetNowPlayingAsync(CancellationToken ct)
    {
        try
        {
            string raw = await spotify.GetNowPlayingAsync(ct);
            string[] parts = raw.Split(" by ", 2);
            return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "");
        }
        catch
        {
            return ("", "");
        }
    }

    private static OverlayWar BuildWarOverlay(RunTimeWar war)
    {
        // A war without both clan blocks carries nothing to render (happens on "notInWar" placeholders).
        if (war.Clan == null || war.Opponent == null) return new OverlayWar();

        return new OverlayWar
        {
            Home = BuildTeamOverlay(war.Clan, "home"),
            Away = BuildTeamOverlay(war.Opponent, "away"),
            Stats = BuildWarStats(war)
        };
    }

    private static OverlayTeam BuildTeamOverlay(RunTimeClan clan, string side) => new()
    {
        Name = clan.Name,
        LogoFile = $"logo {side}.png",
        Stars = clan.Stars,
        // Period decimal regardless of host culture; the overlay shows this string verbatim.
        DestructionPct = clan.DestructionPercentage.ToString("0.0", CultureInfo.InvariantCulture) + "%",
        // Members is null on a "notInWar" placeholder response from the CoC API; treat it as an empty roster
        // rather than letting LINQ throw ArgumentNullException into the 5-second writer loop.
        Players = (clan.Members ?? [])
            .OrderBy(m => m.MapPosition)
            .Select(m =>
            {
                RunTimeAttack? atk = m.Attacks?.FirstOrDefault();
                return new OverlayPlayer
                {
                    Name = m.Name,
                    MapPosition = m.MapPosition,
                    Stars = atk?.Stars,
                    DestructionPct = atk != null ? atk.DestructionPercentage + "%" : null,
                    Duration = atk != null
                        ? $"{(int)atk.Duration / 60}:{(int)atk.Duration % 60:D2}"
                        : null
                };
            })
            .ToList()
    };

    private static OverlayWarStats BuildWarStats(RunTimeWar war)
    {
        int maxPerTeam = war.TeamSize * war.AttacksPerMember;
        return new OverlayWarStats
        {
            AttacksUsedHome = $"{war.Clan.Attacks}/{maxPerTeam}",
            AttacksUsedAway = $"{war.Opponent.Attacks}/{maxPerTeam}",
            AvgTimeHome = CalculateAvgTime(war.Clan),
            AvgTimeAway = CalculateAvgTime(war.Opponent),
            HitRateHome = CalculateHitRate(war.Clan),
            HitRateAway = CalculateHitRate(war.Opponent)
        };
    }

    private static string CalculateAvgTime(RunTimeClan clan)
    {
        List<double> durations = (clan.Members ?? [])
            .Where(m => m.Attacks?.Any() == true)
            .SelectMany(m => m.Attacks)
            .Select(a => a.Duration)
            .ToList();

        if (durations.Count == 0) return "";

        double avg = durations.Average();
        return $"{(int)avg / 60}:{(int)avg % 60:D2}";
    }

    internal static int CalculateHitRate(RunTimeClan clan)
    {
        var attacks = (clan.Members ?? [])
            .Where(m => m.Attacks != null)
            .SelectMany(m => m.Attacks)
            .ToList();
        if (attacks.Count == 0) return 0;
        int triples = attacks.Count(a => a.Stars == 3);
        return (int)Math.Round((double)triples / attacks.Count * 100);
    }

    private void LoadEventConfig()
    {
        if (!File.Exists(ConfigPath)) return;
        try
        {
            string json = File.ReadAllText(ConfigPath);
            _eventConfig = JsonSerializer.Deserialize<OverlayEventConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Failed to load event config: {ex.Message}");
        }
    }

    private void StartConfigWatcher()
    {
        string dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(dir)) return;

        _configWatcher = new FileSystemWatcher(dir, Path.GetFileName(ConfigPath))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _configWatcher.Changed += OnConfigFileChanged;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(_stopToken);
        CancellationTokenSource? old = Interlocked.Exchange(ref _watcherDebounce, newCts);
        old?.Cancel();
        old?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, newCts.Token);
                LoadEventConfig();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"[Overlay] Error reloading event config: {ex.Message}");
            }
        }, newCts.Token);
    }

    public override void Dispose()
    {
        _configWatcher?.Dispose();
        _watcherDebounce?.Dispose();
        _writeLock.Dispose();
        base.Dispose();
    }
}
