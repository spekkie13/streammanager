using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;

public class CustomTwitchHttpClient : ICustomTwitchHttpClient, ITwitchChannelInfoClient
{
    private readonly HttpClient _Client = new();
    private readonly ITwitchAuthTokenProvider _Tokens;
    private string? _ChannelId;
    
    public CustomTwitchHttpClient(ITwitchAuthTokenProvider tokens)
    {
        _Tokens = tokens;
    }

    private async Task EnsureHeadersAsync(CancellationToken cancellationToken)
    {
        string clientId = await _Tokens.GetClientIdAsync(cancellationToken);
        string accessToken = await _Tokens.GetUserAccessTokenAsync(cancellationToken);
        TwitchGeneralFile file = await _Tokens.ReadIdentityAsync(cancellationToken);
        _ChannelId = file.ChannelId;
        
        _Client.DefaultRequestHeaders.Remove("client-id");
        _Client.DefaultRequestHeaders.Remove("broadcaster_id");
        
        _Client.DefaultRequestHeaders.Add("client-id", clientId);
        _Client.DefaultRequestHeaders.Add("broadcaster_id", _ChannelId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        await EnsureHeadersAsync(ct);
        
        HttpResponseMessage response = await send();
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
        await EnsureHeadersAsync(ct);
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={_ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"]);
    }

    public async Task<string> GetLatestFollower(CancellationToken ct = default)
    {
        await EnsureHeadersAsync(ct);
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={_ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["user_name"]?.ToString() ?? "N/A";
    }

    public async Task<int> GetSubscriberCount(CancellationToken ct = default)
    {
        await EnsureHeadersAsync(ct);
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={_ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"]);
    }

    public async Task<string> GetLatestSubscriber(CancellationToken ct = default)
    {
        await EnsureHeadersAsync(ct);
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={_ChannelId}&first=100";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        JArray? data = json["data"] as JArray;
        if (data == null || data.Count == 0) return "N/A";
        // API returns oldest-first; broadcaster's own sub is always last — skip it
        var subscribers = data.Where(s => s["user_id"]?.ToString() != _ChannelId).ToList();
        return subscribers.LastOrDefault()?["user_name"]?.ToString() ?? "N/A";
    }

    public async Task<string?> GetCurrentStreamIdAsync(CancellationToken ct = default)
    {
        await EnsureHeadersAsync(ct);
        string url = $"{TwitchConstants.TwitchStreamsUrl}?user_id={_ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return null;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["id"]?.ToString();
    }
}