using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.StreamStats;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class FollowSubFeature : IDisposable
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchFileWriter _Files;
    private readonly ITwitchFileReader _FileReader;
    private readonly ITwitchChannelInfoClient _Api;
    private readonly StreamStatsClient _StreamStats;
    private readonly ISpotifyService _Spotify;

    private FileSystemWatcher? _GoalsWatcher;
    private CancellationTokenSource? _WatcherDebounce;
    private CancellationTokenSource? _MusicResumeDebounce;
    private CancellationToken _StopToken;
    private int _CurrentSubCount;

    public FollowSubFeature(
        ITwitchChat chat,
        ITwitchChannelInfoClient api,
        ITwitchFileWriter files,
        ITwitchFileReader fileReader,
        StreamStatsClient streamStats,
        ISpotifyService spotify
    ) {
        _Chat = chat;
        _Api = api;
        _Files = files;
        _FileReader = fileReader;
        _StreamStats = streamStats;
        _Spotify = spotify;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _StopToken = cancellationToken;

        string latestFollower = await _Api.GetLatestFollower(cancellationToken);
        string latestSubscriberRaw = await _FileReader.ReadLatestSubDisplayAsync();
        string latestSubscriber = string.IsNullOrWhiteSpace(latestSubscriberRaw)
            ? await _Api.GetLatestSubscriber(cancellationToken)
            : latestSubscriberRaw;
        int totalFollowers = await _Api.GetFollowerCount(cancellationToken);

        int? streamStatsCount = await _StreamStats.GetSubCountAsync(cancellationToken);
        _CurrentSubCount = streamStatsCount ?? await _Api.GetSubscriberCount(cancellationToken);

        _Files.WriteLatestFollowerHtml(latestFollower, totalFollowers);
        _Files.WriteLatestSubHtml(latestSubscriber, _CurrentSubCount);
        await WriteSubGoalAsync(_CurrentSubCount, cancellationToken);

        StartGoalsWatcher();
    }

    private void StartGoalsWatcher()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "SpekkieTwitchBot", "Settings");

        _GoalsWatcher = new FileSystemWatcher(dir, "goals.json")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _GoalsWatcher.Changed += OnGoalsConfigChanged;
    }

    private void OnGoalsConfigChanged(object sender, FileSystemEventArgs e)
    {
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        CancellationTokenSource? oldCts = Interlocked.Exchange(ref _WatcherDebounce, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, newCts.Token);
                StreamGoalsConfig? config = await _FileReader.ReadGoalsConfigAsync();
                if (config == null) return;
                _Files.WriteSubGoalHtml(config);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading goals config: {ex}");
            }
        }, newCts.Token);
    }

    private void DebounceMusicResume()
    {
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        CancellationTokenSource? oldCts = Interlocked.Exchange(ref _MusicResumeDebounce, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await _Spotify.PausePlayerAsync(newCts.Token);
                await Task.Delay(TimeSpan.FromSeconds(25), newCts.Token);
                await _Spotify.ResumePlayerAsync(newCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in music pause/resume flow: {ex}");
            }
        }, newCts.Token);
    }

    public void Dispose()
    {
        _GoalsWatcher?.Dispose();
        _WatcherDebounce?.Cancel();
        _WatcherDebounce?.Dispose();
        _MusicResumeDebounce?.Cancel();
        _MusicResumeDebounce?.Dispose();
    }

    public async Task HandleFollowAsync(FollowHappened e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(e.UserName)) return;
        
        await _Files.WriteMostRecentFollowerAsync(e.UserName, cancellationToken);
        int totalFollowers = await _Api.GetFollowerCount(cancellationToken);
        await _Files.WriteTotalFollowersAsync(totalFollowers, cancellationToken);
        _Files.WriteLatestFollowerHtml(e.UserName, totalFollowers);
        
        await _Chat.SendAsync(message: $"Thanks for the follow {e.UserName}", cancellationToken);
    }

    public async Task HandleSubAsync(SubHappened e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(e.RecipientUserName)) return;

        DebounceMusicResume();

        string latestSubscriber = FormatLatestSub(e);
        await _Files.WriteMostRecentSubscriberAsync(latestSubscriber, cancellationToken);

        int increment = e.Kind == SubKind.CommunityGift ? e.GiftCount : 1;
        _CurrentSubCount += increment;

        await _Files.WriteTotalSubscribersAsync(_CurrentSubCount, cancellationToken);
        _Files.WriteLatestSubHtml(latestSubscriber, _CurrentSubCount);
        await WriteSubGoalAsync(_CurrentSubCount, cancellationToken);

        await _Chat.SendAsync(message: FormatChatThanks(e), cancellationToken);
    }

    private async Task WriteSubGoalAsync(int current, CancellationToken ct)
    {
        StreamGoalsConfig? config = await _FileReader.ReadGoalsConfigAsync();
        if (config == null) return;
        StreamGoalsConfig updated = config with { SubGoal = config.SubGoal with { CurrentAmount = current } };
        _Files.WriteGoalsConfig(updated);
        _Files.WriteSubGoalHtml(updated);
    }

    private static string FormatLatestSub(SubHappened e)
    {
        return e.Kind switch
        {
            SubKind.New => $"{e.RecipientUserName} subscribed (Tier {HumanTier(e.Tier)})",
            SubKind.Resub => $"{e.RecipientUserName} resubbed ({e.TotalMonths ?? 0} months, Tier {HumanTier(e.Tier)})",
            SubKind.Gift => $"{e.GifterUserName ?? "Someone"} gifted a sub to {e.RecipientUserName} (Tier {HumanTier(e.Tier)})",
            SubKind.CommunityGift => $"{e.GifterUserName ?? "Someone"} gifted subs to the community",
            SubKind.PrimePaidUpgrade => $"{e.RecipientUserName} upgraded from Prime",
            SubKind.ContinuedGift => $"{e.RecipientUserName} continued a gifted sub",
            _ => $"{e.RecipientUserName} subscribed"
        };
    }

    private static string HumanTier(string tier) =>
        tier switch
        {
            "prime" => "Prime",
            "1000" => "1",
            "2000" => "2",
            "3000" => "3",
            _ => tier
        };
    
    private static string FormatChatThanks(SubHappened e)
    {
        return e.Kind switch
        {
            SubKind.New => $"Welcome {e.RecipientUserName}! Thanks for subscribing ❤️",
            SubKind.Resub => $"Welcome back {e.RecipientUserName}! ❤️ ({e.TotalMonths ?? 0} months!)",
            SubKind.Gift => $"Huge thanks {e.GifterUserName ?? "friend"} for gifting a sub to {e.RecipientUserName}! 🎁",
            SubKind.CommunityGift => $"Insane! Thanks {e.GifterUserName ?? "friend"} for the community gift! 🎁",
            SubKind.PrimePaidUpgrade => $"Thanks for upgrading, {e.RecipientUserName}! ❤️",
            SubKind.ContinuedGift => $"Love it {e.RecipientUserName} — thanks for continuing the gifted sub! ❤️",
            _ => $"Thanks for subscribing, {e.RecipientUserName}! ❤️"
        };
    }
}