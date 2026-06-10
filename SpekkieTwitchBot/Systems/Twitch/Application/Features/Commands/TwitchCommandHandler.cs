using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.BaseReview;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;
using SpekkieTwitchBot.Systems.Twitch.Models.BaseReview;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TwitchCommandHandler(
    ChannelPointsFeature channelPointsFeature,
    ITwitchChannelInfoClient api,
    BaseReviewQueueService baseReviewQueue)
    : ITwitchCommandHandler
{
    public async Task<string> HandleCreateRedemptionCommand(string commandArgs)
        => await channelPointsFeature.CreateRedemption(commandArgs);

    public async Task<string> HandleUptimeCommand(CancellationToken ct)
    {
        DateTimeOffset? startTime = await api.GetStreamStartTimeAsync(ct);
        if (startTime == null)
            return "The stream is currently offline. | De stream is momenteel offline.";

        TimeSpan uptime = DateTimeOffset.UtcNow - startTime.Value;
        int hours = (int)uptime.TotalHours;
        int minutes = uptime.Minutes;

        string en = hours > 0
            ? $"Stream has been live for {hours}h {minutes}m"
            : $"Stream has been live for {minutes}m";
        string nl = hours > 0
            ? $"Stream is al {hours}u {minutes}m live"
            : $"Stream is al {minutes}m live";

        return $"{en} | {nl}";
    }

    public async Task<string> HandleClipCommand(CancellationToken ct)
    {
        string? clipUrl = await api.CreateClipAsync(ct);
        return clipUrl != null
            ? $"Clip created! {clipUrl}"
            : "Failed to create clip — is the stream live?";
    }

    public async Task<string> HandleShoutoutCommand(string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Usage: !so <username>";

        (string? lastGame, string? login) = await api.GetShoutoutInfoAsync(username.TrimStart('@'), ct);
        if (login == null)
            return $"Could not find user '{username}'.";

        string gameEn = string.IsNullOrWhiteSpace(lastGame) ? "something awesome" : lastGame;
        string gameNl = string.IsNullOrWhiteSpace(lastGame) ? "iets tofs" : lastGame;

        return $"Go check out @{login} — last seen playing {gameEn}! twitch.tv/{login} " +
               $"| Ga eens kijken bij @{login} — laatst gezien met {gameNl}! twitch.tv/{login}";
    }

    public async Task<string> HandleSubShoutoutCommand(string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Usage: !subso <username>";

        (string? login, string? lastGame, bool isSubscriber, string? tier) =
            await api.GetSubscriberShoutoutInfoAsync(username.TrimStart('@'), ct);
        if (login == null)
            return $"Could not find user '{username}'.";

        string gameEn = string.IsNullOrWhiteSpace(lastGame) ? "something awesome" : lastGame;
        string gameNl = string.IsNullOrWhiteSpace(lastGame) ? "iets tofs" : lastGame;

        if (!isSubscriber)
            return $"Go check out @{login} — last seen playing {gameEn}! twitch.tv/{login} " +
                   $"| Ga eens kijken bij @{login} — laatst gezien met {gameNl}! twitch.tv/{login}";

        string tierLabel = HumanTier(tier);
        return $"Massive shoutout to our Tier {tierLabel} sub @{login} ❤️ — last seen playing {gameEn}! Go follow: twitch.tv/{login} " +
               $"| Dikke shoutout naar onze Tier {tierLabel} sub @{login} ❤️ — laatst gezien met {gameNl}! Ga volgen: twitch.tv/{login}";
    }

    public async Task<string> HandleNextBaseCommand(CancellationToken ct)
    {
        BaseReviewEntry? next = await baseReviewQueue.DequeueAsync(ct);
        if (next == null)
            return "The base-review queue is empty.";

        string subLabel = next.IsSubscriber ? $" (Tier {HumanTier(next.Tier)} sub)" : "";
        return $"Next base: @{next.UserName}{subLabel} — {next.Input}";
    }

    public async Task<string> HandleBaseQueueCommand(CancellationToken ct)
    {
        IReadOnlyList<BaseReviewEntry> queue = await baseReviewQueue.SnapshotAsync(ct);
        if (queue.Count == 0)
            return "The base-review queue is empty.";

        const int previewCount = 5;
        string preview = string.Join(", ",
            queue.Take(previewCount).Select((e, i) => $"{i + 1}. @{e.UserName}{(e.IsSubscriber ? " (sub)" : "")}"));
        string suffix = queue.Count > previewCount ? $" … (+{queue.Count - previewCount} more)" : "";

        return $"Base-review queue ({queue.Count}): {preview}{suffix}";
    }

    private static string HumanTier(string? tier) =>
        tier switch
        {
            "1000" => "1",
            "2000" => "2",
            "3000" => "3",
            "prime" => "Prime",
            null or "" => "1",
            _ => tier
        };
}