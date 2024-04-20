using System.Net.Http.Headers;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Models.Twitch.Auth;

namespace SpekkieTwitchBot.Twitch.General;

public class CustomTwitchHttpClient
{
    private readonly HttpClient _Client;
    private readonly TwitchAuthService _TwitchAuthService;

    public CustomTwitchHttpClient(
        HttpClient client,
        TwitchAuthService twitchAuthService)
    {
        _Client = client;
        _TwitchAuthService = twitchAuthService;
        Setup();
    }

    private void Setup()
    {
        TwitchUserAuth auth = _TwitchAuthService.GetTwitchUserAuth();
        GeneralTwitchAuth genAuth = _TwitchAuthService.GetGeneralTwitchAuth();
        
        _Client.DefaultRequestHeaders.Add("client-id", auth.ClientId);
        _Client.DefaultRequestHeaders.Add("broadcaster_id", genAuth.ChannelId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.UserToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _Client.GetAsync(url);
    }

    public async Task<HttpResponseMessage> PatchAsync(string url, StringContent content)
    {
        return await _Client.PatchAsync(url, content);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, FormUrlEncodedContent content)
    {
        return await _Client.PostAsync(url, content);
    }    
    
    public async Task<HttpResponseMessage> PostAsync(string url, StringContent content)
    {
        return await _Client.PostAsync(url, content);
    }
}