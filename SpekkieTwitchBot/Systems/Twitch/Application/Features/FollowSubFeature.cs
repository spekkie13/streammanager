using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class FollowSubFeature(
    ITwitchChat chat,
    ITwitchChannelInfoClient api,
    ITwitchFileWriter files,
    ITwitchFileReader fileReader,
    ISpotifyService spotify,
    Logger logger)
    : IDisposable
{
    private FileSystemWatcher? _GoalsWatcher;
    private CancellationTokenSource? _WatcherDebounce;
    private CancellationTokenSource? _MusicResumeDebounce;
    private CancellationToken _StopToken;
    private int _CurrentSubCount;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _StopToken = cancellationToken;

        string latestFollower = await api.GetLatestFollower(cancellationToken);
        string latestSubscriberRaw = await fileReader.ReadLatestSubDisplayAsync();
        string latestSubscriber = string.IsNullOrWhiteSpace(latestSubscriberRaw)
            ? await api.GetLatestSubscriber(cancellationToken)
            : latestSubscriberRaw;
        int totalFollowers = await api.GetFollowerCount(cancellationToken);

        StreamGoalsConfig? goalsConfig = await fileReader.ReadGoalsConfigAsync();
        _CurrentSubCount = goalsConfig?.SubGoal.CurrentAmount ?? 0;

        files.WriteLatestFollowerHtml(latestFollower, totalFollowers);
        files.WriteLatestSubHtml(latestSubscriber, _CurrentSubCount);
        if (goalsConfig != null) files.WriteSubGoalHtml(goalsConfig);

        StartGoalsWatcher();
    }

    private void StartGoalsWatcher()
    {
        string dir = Path.Combine(BotPaths.BaseDir, "Settings");

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
                StreamGoalsConfig? config = await fileReader.ReadGoalsConfigAsync();
                if (config == null) return;
                files.WriteSubGoalHtml(config);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"Error reloading goals config: {ex}");
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
                await spotify.PausePlayerAsync(newCts.Token);
                await Task.Delay(TimeSpan.FromSeconds(25), newCts.Token);
                await spotify.ResumePlayerAsync(newCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"Error in music pause/resume flow: {ex}");
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
        
        await files.WriteMostRecentFollowerAsync(e.UserName, cancellationToken);
        int totalFollowers = await api.GetFollowerCount(cancellationToken);
        await files.WriteTotalFollowersAsync(totalFollowers, cancellationToken);
        files.WriteLatestFollowerHtml(e.UserName, totalFollowers);
        
        await chat.SendAsync(message: $"Thanks for the follow {e.UserName}", cancellationToken);
    }

    public async Task HandleSubAsync(SubHappened e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(e.RecipientUserName)) return;

        DebounceMusicResume();

        string latestSubscriber = FormatLatestSub(e);
        await files.WriteMostRecentSubscriberAsync(latestSubscriber, cancellationToken);

        int increment = e.Kind == SubKind.CommunityGift ? e.GiftCount : 1;
        _CurrentSubCount += increment;

        await files.WriteTotalSubscribersAsync(_CurrentSubCount, cancellationToken);
        files.WriteLatestSubHtml(latestSubscriber, _CurrentSubCount);
        await WriteSubGoalAsync(_CurrentSubCount, cancellationToken);

        await chat.SendAsync(message: FormatChatThanks(e), cancellationToken);
    }

    private async Task WriteSubGoalAsync(int current, CancellationToken ct)
    {
        StreamGoalsConfig? config = await fileReader.ReadGoalsConfigAsync();
        if (config == null) return;
        StreamGoalsConfig updated = config with { SubGoal = config.SubGoal with { CurrentAmount = current } };
        files.WriteGoalsConfig(updated);
        files.WriteSubGoalHtml(updated);
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