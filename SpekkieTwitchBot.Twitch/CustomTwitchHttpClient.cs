using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Auth;

namespace TwitchAuthService;

public sealed class CustomTwitchHttpClient
{
    private readonly HttpClient _Client;
    private readonly TwitchAuthService _TwitchAuthService;

    public CustomTwitchHttpClient(TwitchAuthService twitchAuthService)
    {
        _Client = new HttpClient();
        _TwitchAuthService = twitchAuthService;
        Setup();
    }

    private void Setup()
    {
        TwitchUserAuth? auth = _TwitchAuthService.GetTwitchUserAuth();
        GeneralTwitchAuth? genAuth = _TwitchAuthService.GetGeneralTwitchAuth();


        _Client.DefaultRequestHeaders.Add("client-id", auth.ClientId);
        _Client.DefaultRequestHeaders.Add("broadcaster_id", genAuth.ChannelId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.UserToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        HttpResponseMessage response = await _Client.GetAsync(url);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
        Setup();
        response = await _Client.GetAsync(url);
        return response;
    }

    public async Task<HttpResponseMessage> PatchAsync(string url, StringContent content)
    {
        HttpResponseMessage response = await _Client.PatchAsync(url, content);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
        Setup();
        response = await _Client.PatchAsync(url, content);
        return response;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, StringContent content)
    {
        HttpResponseMessage response = await _Client.PostAsync(url, content);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
        Setup();
        response = await _Client.PostAsync(url, content);
        return response;
    }
    
    public async Task<int> GetFollowerCount()  
    {
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        
        JObject jsonObject = JObject.Parse(response);
        int followerCount = Convert.ToInt32(jsonObject["total"]);

        return followerCount;
    }

    public async Task<string> GetLatestFollower()
    {
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        
        JObject jsonObject = JObject.Parse(response);
        string firstUserName = jsonObject["data"]?[0]?["user_name"]?.ToString() ?? "N/A";

        return firstUserName;
    }
    
    public async Task<int> GetSubscriberCount()
    {
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        
        JObject jsonObject = JObject.Parse(response);
        int subscriberCount = Convert.ToInt32(jsonObject["total"]);
        
        return subscriberCount;
    }

    public async Task<string> GetLatestSubscriber()
    {
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        
        JObject jsonObject = JObject.Parse(response);
        string firstUserName = jsonObject["data"]?[0]?["user_name"]?.ToString() ?? "N/A";

        return firstUserName;
    }
}