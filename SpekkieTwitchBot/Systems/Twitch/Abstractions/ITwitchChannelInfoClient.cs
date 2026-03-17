namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchChannelInfoClient
{
    Task<int> GetFollowerCount(CancellationToken cancellationToken = default);
    Task<string> GetLatestFollower(CancellationToken cancellationToken = default);
    Task<int> GetSubscriberCount(CancellationToken cancellationToken = default);
    Task<string> GetLatestSubscriber(CancellationToken cancellationToken = default);
}
