using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Events;
using SpekkieClassLibrary.Overlay;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Overlay;

public class OverlayStateService(
    WarService warService,
    OverlayModeController modeController,
    SpotlightSelectionReader selectionReader,
    ITwitchChannelInfoClient twitchApi,
    ITwitchAuthTokenProvider tokens,
    ISpotifyService spotify,
    IStreamEventBus eventBus,
    Logger logger)
    : BackgroundService
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

    private OverlayEventConfig _eventConfig = new();
    private string _accountName = "";
    private string _latestFollower = "";
    private int _followerCount;
    private string _latestSub = "";
    private int _subscriberCount;
    private FileSystemWatcher? _configWatcher;
    private CancellationTokenSource? _watcherDebounce;
    private CancellationToken _stopToken;

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

    private async Task WriteOverlayStateAsync(CancellationToken ct)
    {
        try
        {
            (string title, string artist) = await GetNowPlayingAsync(ct);
            RunTimeWar? war = warService.LastKnownWar;

            OverlayState state = new()
            {
                Mode = modeController.Resolve(),
                UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                IsWarActive = warService.IsWarActive,
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
    }

    // Live Attack Spotlight: resolve the StreamDeck selection against the current in-memory war and
    // publish a self-contained active-player.json. The current war is the source of truth — we never
    // read war-roster.json back. Any of: no selection, no active war, or a missing position yields an
    // inactive payload. Never throws into the loop.
    private async Task WriteActivePlayerAsync(CancellationToken ct)
    {
        try
        {
            ActivePlayer activePlayer = BuildActivePlayer();
            string json = JsonSerializer.Serialize(activePlayer, ActivePlayerOptions);
            await OverlayJson.WriteAtomicAsync(ActivePlayerPath, json, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"[Overlay] Error writing active player: {ex.Message}");
        }
    }

    private ActivePlayer BuildActivePlayer()
    {
        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        SpotlightSelection? selection = selectionReader.Read();
        return SpotlightMapper.BuildActivePlayer(
            warService.LastKnownWar, warService.IsWarActive, selection?.Team, selection?.Position, now);
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

    private static OverlayWar BuildWarOverlay(RunTimeWar war) => new()
    {
        Home = BuildTeamOverlay(war.Clan, "home"),
        Away = BuildTeamOverlay(war.Opponent, "away"),
        Stats = BuildWarStats(war)
    };

    private static OverlayTeam BuildTeamOverlay(RunTimeClan clan, string side) => new()
    {
        Name = clan.Name,
        LogoFile = $"logo {side}.png",
        Stars = clan.Stars,
        // Period decimal regardless of host culture; the overlay shows this string verbatim.
        DestructionPct = clan.DestructionPercentage.ToString("0.0", CultureInfo.InvariantCulture) + "%",
        Players = clan.Members
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
            HitRateHome = CalculateHitRate(war.Clan, war.TeamSize),
            HitRateAway = CalculateHitRate(war.Opponent, war.TeamSize)
        };
    }

    private static string CalculateAvgTime(RunTimeClan clan)
    {
        List<double> durations = clan.Members
            .Where(m => m.Attacks?.Any() == true)
            .SelectMany(m => m.Attacks)
            .Select(a => a.Duration)
            .ToList();

        if (durations.Count == 0) return "";

        double avg = durations.Average();
        return $"{(int)avg / 60}:{(int)avg % 60:D2}";
    }

    private static int CalculateHitRate(RunTimeClan clan, int teamSize)
    {
        if (teamSize == 0) return 0;
        int hits = clan.Members.Count(m => m.Attacks?.Any() == true);
        return (int)Math.Round((double)hits / teamSize * 100);
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
        base.Dispose();
    }
}
