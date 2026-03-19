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
}
