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
    private readonly SemaphoreSlim _HeaderLock = new(1, 1);
    
    public CustomTwitchHttpClient(ITwitchAuthTokenProvider tokens)
    {
        _Tokens = tokens;
    }

    private async Task EnsureHeadersAsync(CancellationToken cancellationToken)
    {
        string clientId = await _Tokens.GetClientIdAsync(cancellationToken);
        string accessToken = await _Tokens.GetUserAccessTokenAsync(cancellationToken);

        _Client.DefaultRequestHeaders.Remove("client-id");
        _Client.DefaultRequestHeaders.Remove("broadcaster_id");
        
        if (string.IsNullOrEmpty(clientId)) throw new InvalidOperationException("Twitch ClientId is missing from auth file.");
        if (string.IsNullOrEmpty(accessToken)) throw new InvalidOperationException("Twitch access token is missing or could not be refreshed.");

        _Client.DefaultRequestHeaders.Add("client-id", clientId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        await _HeaderLock.WaitAsync(ct);
        try
        {
            await EnsureHeadersAsync(ct);

            HttpResponseMessage response = await send();
            if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

            await _Tokens.ForceRefreshAsync(ct);
            await EnsureHeadersAsync(ct);

            response.Dispose();
            return await send();
        }
        finally
        {
            _HeaderLock.Release();
        }
    }
    
    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.GetAsync(url, ct), ct);

    public Task<HttpResponseMessage> PostAsync(string url, StringContent content, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.PostAsync(url, content, ct), ct);

    public Task<HttpResponseMessage> PatchAsync(string url, StringContent content, CancellationToken ct = default)
        => SendWithRetryAsync(() => _Client.PatchAsync(url, content, ct), ct);

    public async Task<int> GetFollowerCount(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={identity.ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return 0;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"] ?? 0);
    }

    public async Task<string> GetLatestFollower(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={identity.ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return "N/A";
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["user_name"]?.ToString() ?? "N/A";
    }

    public async Task<int> GetSubscriberCount(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={identity.ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return 0;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return Convert.ToInt32(json["total"] ?? 0);
    }

    public async Task<string> GetLatestSubscriber(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={identity.ChannelId}&first=100";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return "N/A";
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        JArray? data = json["data"] as JArray;
        if (data == null || data.Count == 0) return "N/A";
        // API returns oldest-first; broadcaster's own sub is always last — skip it
        var subscribers = data.Where(s => s["user_id"]?.ToString() != identity.ChannelId).ToList();
        return subscribers.LastOrDefault()?["user_name"]?.ToString() ?? "N/A";
    }

    public async Task<string?> GetCurrentStreamIdAsync(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchStreamsUrl}?user_id={identity.ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return null;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        return json["data"]?[0]?["id"]?.ToString();
    }

    public async Task<DateTimeOffset?> GetStreamStartTimeAsync(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchStreamsUrl}?user_id={identity.ChannelId}";
        using HttpResponseMessage msg = await GetAsync(url, ct);
        if (!msg.IsSuccessStatusCode) return null;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        string? startedAt = json["data"]?[0]?["started_at"]?.ToString();
        if (startedAt == null) return null;
        return DateTimeOffset.TryParse(startedAt, null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTimeOffset result)
            ? result.ToUniversalTime()
            : null;
    }

    public async Task<string?> CreateClipAsync(CancellationToken ct = default)
    {
        TwitchGeneralFile identity = await _Tokens.ReadIdentityAsync(ct);
        string url = $"{TwitchConstants.TwitchClipsUrl}?broadcaster_id={identity.ChannelId}";
        using HttpResponseMessage msg = await PostAsync(url, new StringContent(""), ct);
        if (!msg.IsSuccessStatusCode) return null;
        JObject json = JObject.Parse(await msg.Content.ReadAsStringAsync(ct));
        string? id = json["data"]?[0]?["id"]?.ToString();
        return id == null ? null : $"https://clips.twitch.tv/{id}";
    }

    public async Task<(string? LastGame, string? Login)> GetShoutoutInfoAsync(string username, CancellationToken ct = default)
    {
        string userUrl = $"{TwitchConstants.TwitchUsersUrl}?login={username}";
        using HttpResponseMessage userMsg = await GetAsync(userUrl, ct);
        if (!userMsg.IsSuccessStatusCode) return (null, null);
        JObject userJson = JObject.Parse(await userMsg.Content.ReadAsStringAsync(ct));
        string? userId = userJson["data"]?[0]?["id"]?.ToString();
        string? login = userJson["data"]?[0]?["login"]?.ToString();
        if (userId == null) return (null, null);

        string channelUrl = $"{TwitchConstants.TwitchChannelsUrl}?broadcaster_id={userId}";
        using HttpResponseMessage chanMsg = await GetAsync(channelUrl, ct);
        if (!chanMsg.IsSuccessStatusCode) return (null, login);
        JObject chanJson = JObject.Parse(await chanMsg.Content.ReadAsStringAsync(ct));
        string? lastGame = chanJson["data"]?[0]?["game_name"]?.ToString();
        return (lastGame, login);
    }
}