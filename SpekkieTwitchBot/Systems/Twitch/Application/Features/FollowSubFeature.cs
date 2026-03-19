using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class FollowSubFeature : IDisposable
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchFileWriter _Files;
    private readonly ITwitchFileReader _FileReader;
    private readonly ITwitchChannelInfoClient _Api;

    private FileSystemWatcher? _GoalsWatcher;
    private CancellationTokenSource? _WatcherDebounce;
    private CancellationToken _StopToken;

    public FollowSubFeature(
        ITwitchChat chat,
        ITwitchChannelInfoClient api,
        ITwitchFileWriter files,
        ITwitchFileReader fileReader
    ) {
        _Chat = chat;
        _Api = api;
        _Files = files;
        _FileReader = fileReader;
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
        int totalSubs = await _Api.GetSubscriberCount(cancellationToken);
        _Files.WriteLatestFollowerHtml(latestFollower, totalFollowers);
        _Files.WriteLatestSubHtml(latestSubscriber, totalSubs);
        await WriteSubGoalAsync(totalSubs, cancellationToken);

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
        _WatcherDebounce?.Cancel();
        _WatcherDebounce?.Dispose();
        _WatcherDebounce = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        var cts = _WatcherDebounce;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, cts.Token);
                StreamGoalsConfig? config = await _FileReader.ReadGoalsConfigAsync();
                if (config == null) return;
                _Files.WriteSubGoalHtml(config);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading goals config: {ex.Message}");
            }
        }, cts.Token);
    }

    public void Dispose()
    {
        _GoalsWatcher?.Dispose();
        _WatcherDebounce?.Dispose();
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

        string latestSubscriber = FormatLatestSub(e);
        await _Files.WriteMostRecentSubscriberAsync(latestSubscriber, cancellationToken);
        int totalSubs = await _Api.GetSubscriberCount(cancellationToken);
        await _Files.WriteTotalSubscribersAsync(totalSubs, cancellationToken);
        _Files.WriteLatestSubHtml(latestSubscriber, totalSubs);
        await WriteSubGoalAsync(totalSubs, cancellationToken);

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