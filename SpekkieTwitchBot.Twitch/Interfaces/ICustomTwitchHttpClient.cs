namespace TwitchAuthService.Interfaces;

public interface ICustomTwitchHttpClient
{
    Task<HttpResponseMessage> GetAsync(string url);
    Task<HttpResponseMessage> PatchAsync(string url, StringContent content);
    Task<HttpResponseMessage> PostAsync(string url, StringContent content);

    Task<int> GetFollowerCount();
    Task<int> GetSubscriberCount();
    Task<string> GetLatestFollower();
    Task<string> GetLatestSubscriber();
}