using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using TwitchAuthService.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch;

public class CustomTwitchHttpClient : ICustomTwitchHttpClient
{
    private readonly HttpClient _Client;
    private readonly ITwitchAuthTokenProvider _Tokens;

    public CustomTwitchHttpClient(
        ITwitchAuthTokenProvider tokens
    ) {
        _Client = new HttpClient();
        _Tokens = tokens;
    }

    private async Task EnsureHeadersAsync(CancellationToken cancellationToken)
    {
        var clientId = await _Tokens.GetClientIdAsync(cancellationToken);
        var accessToken = await _Tokens.GetUserAccessTokenAsync(cancellationToken);
        
        _Client.DefaultRequestHeaders.Remove("client-id");
        _Client.DefaultRequestHeaders.Remove("broadcaster_id");
        
        _Client.DefaultRequestHeaders.Add("client-id", clientId);
        _Client.DefaultRequestHeaders.Add("broadcaster_id", TwitchConstants.BroadcasterId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        await EnsureHeadersAsync(ct);
        
        var response = await send();
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        
        await _Tokens.ForceRefreshAsync(ct);
        await EnsureHeadersAsync(ct);
        
        response.Dispose();
        return await send();
    }
    
    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.GetAsync(url, ct), ct);

    public Task<HttpResponseMessage> PostAsync(string url, StringContent content, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.PostAsync(url, content, ct), ct);

    public Task<HttpResponseMessage> PatchAsync(string url, StringContent content, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.PatchAsync(url, content, ct), ct);

    public async Task<int> GetFollowerCount(CancellationToken ct = default)
    {
        var url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        using var msg = await GetAsync(url, ct);
        var json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"]);
    }

    public async Task<string> GetLatestFollower(CancellationToken ct = default)
    {
        var url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        using var msg = await GetAsync(url, ct);
        var json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["user_name"]?.ToString() ?? "N/A";
    }

    public async Task<int> GetSubscriberCount(CancellationToken ct = default)
    {
        var url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        using var msg = await GetAsync(url, ct);
        var json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"]);
    }

    public async Task<string> GetLatestSubscriber(CancellationToken ct = default)
    {
        var url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        using var msg = await GetAsync(url, ct);
        var json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["user_name"]?.ToString() ?? "N/A";
    }
}