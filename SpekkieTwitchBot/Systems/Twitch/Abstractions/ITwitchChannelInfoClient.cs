namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchChannelInfoClient
{
    Task<int> GetFollowerCount(CancellationToken cancellationToken = default);
    Task<string> GetLatestFollower(CancellationToken cancellationToken = default);
    Task<int> GetSubscriberCount(CancellationToken cancellationToken = default);
    Task<string> GetLatestSubscriber(CancellationToken cancellationToken = default);
    Task<string?> GetCurrentStreamIdAsync(CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetStreamStartTimeAsync(CancellationToken ct = default);
    Task<string?> CreateClipAsync(CancellationToken ct = default);
    Task<(string? LastGame, string? Login)> GetShoutoutInfoAsync(string username, CancellationToken ct = default);

    /// <summary>Checks whether a user (by Twitch user id) is a current subscriber of the broadcaster.</summary>
    Task<(bool IsSubscriber, string? Tier)> IsUserSubscribedAsync(string userId, CancellationToken ct = default);

    /// <summary>Resolves a login to its shoutout info plus current subscription status/tier.</summary>
    Task<(string? Login, string? LastGame, bool IsSubscriber, string? Tier)> GetSubscriberShoutoutInfoAsync(
        string username, CancellationToken ct = default);
}
