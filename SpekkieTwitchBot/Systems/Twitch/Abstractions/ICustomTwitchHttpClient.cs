namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ICustomTwitchHttpClient
{
    Task<HttpResponseMessage> GetAsync(
        string url,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> PostAsync(
        string url,
        StringContent content,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> PatchAsync(
        string url,
        StringContent content,
        CancellationToken cancellationToken = default);

    Task<int> GetFollowerCount(
        CancellationToken cancellationToken = default);

    Task<string> GetLatestFollower(
        CancellationToken cancellationToken = default);

    Task<int> GetSubscriberCount(
        CancellationToken cancellationToken = default);

    Task<string> GetLatestSubscriber(
        CancellationToken cancellationToken = default);
}