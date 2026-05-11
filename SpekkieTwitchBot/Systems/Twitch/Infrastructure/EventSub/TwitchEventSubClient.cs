using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.EventSub;

public class TwitchEventSubClient : ITwitchEvents
{
    private readonly Logger _Log;
    private readonly ITwitchAuthTokenProvider _Tokens;
    private readonly ICustomTwitchHttpClient _Http;
    private readonly EventSubWebSocketClient _Ws;
    private readonly Uri _DefaultUri;

    private string? _ChannelId;
    private CancellationToken _RunCt;
    private Uri _ReconnectUri;
    private int _ReconnectAttempt;

    public event Func<FollowHappened, CancellationToken, Task>? OnFollow;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;
    public event Func<BitsHappened, CancellationToken, Task>? OnBits;
    public event Func<ChannelPointRedeemed, CancellationToken, Task>? OnChannelPointRedeemed;

    public TwitchEventSubClient(
        Logger log,
        ITwitchAuthTokenProvider tokens,
        ICustomTwitchHttpClient http,
        EventSubWebSocketClient ws,
        IConfiguration configuration)
    {
        _Log = log;
        _Tokens = tokens;
        _Http = http;
        _Ws = ws;

        string url = configuration["EventSub:WebSocketUrl"] ?? "wss://eventsub.wss.twitch.tv/ws";
        _DefaultUri = new Uri(url);
        _ReconnectUri = _DefaultUri;

        _Log.LogWarning($"[EventSub] WebSocket URL: {url}");

        _Ws.OnConnected += HandleConnected;
        _Ws.OnMessage += HandleMessage;
        _Ws.OnDisconnected += HandleDisconnected;
        _Ws.OnError += ex => _Log.LogError($"[EventSub] WS error: {ex}");
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _RunCt = cancellationToken;
        _ReconnectUri = _DefaultUri;

        var identity = await _Tokens.ReadIdentityAsync(cancellationToken);
        _ChannelId = identity.ChannelId;

        _Log.LogWarning("[EventSub] ConnectAsync starting");
        await _Ws.ConnectAsync(_ReconnectUri, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _Log.LogWarning("[EventSub] DisconnectAsync");
        await _Ws.CloseAsync(cancellationToken);
    }

    private void HandleConnected()
    {
        _Log.LogWarning("[EventSub] Connected — awaiting session_welcome");
    }

    private void HandleDisconnected(string? reason)
    {
        _Log.LogWarning($"[EventSub] Disconnected. Reason={reason ?? "unknown"}");

        _ = Task.Run(async () =>
        {
            TimeSpan delay = NextReconnectDelay();
            _Log.LogWarning($"[EventSub] Reconnect scheduled in {delay.TotalMilliseconds:0}ms");
            await Task.Delay(delay, _RunCt).ConfigureAwait(false);
            await _Ws.ConnectAsync(_ReconnectUri, _RunCt).ConfigureAwait(false);
        }, _RunCt);
    }

    private void HandleMessage(string raw)
    {
        _ = Task.Run(() => ProcessMessageAsync(raw, _RunCt), _RunCt)
            .ContinueWith(
                t => { if (t.Exception != null) _Log.LogError(t.Exception.ToString()); },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ProcessMessageAsync(string raw, CancellationToken ct)
    {
        using JsonDocument doc = JsonDocument.Parse(raw);
        JsonElement root = doc.RootElement;

        string? messageType = root
            .GetProperty("metadata")
            .GetProperty("message_type")
            .GetString();

        switch (messageType)
        {
            case "session_welcome":
            {
                string sessionId = root
                    .GetProperty("payload")
                    .GetProperty("session")
                    .GetProperty("id")
                    .GetString()!;

                _Log.LogWarning($"[EventSub] session_welcome session_id={sessionId}");
                _ReconnectAttempt = 0;
                _ReconnectUri = _DefaultUri;
                await SubscribeToEventsAsync(sessionId, ct);
                break;
            }

            case "session_keepalive":
                break;

            case "notification":
            {
                JsonElement payload = root.GetProperty("payload");
                string? subType = payload
                    .GetProperty("subscription")
                    .GetProperty("type")
                    .GetString();
                JsonElement evt = payload.GetProperty("event");
                await DispatchEventAsync(subType, evt, ct);
                break;
            }

            case "session_reconnect":
            {
                string reconnectUrl = root
                    .GetProperty("payload")
                    .GetProperty("session")
                    .GetProperty("reconnect_url")
                    .GetString()!;

                _Log.LogWarning($"[EventSub] session_reconnect to {reconnectUrl}");
                _ReconnectUri = new Uri(reconnectUrl);
                _Ws.ForceReconnect("session_reconnect");
                break;
            }

            case "revocation":
            {
                string? subType = root
                    .GetProperty("payload")
                    .GetProperty("subscription")
                    .GetProperty("type")
                    .GetString();
                _Log.LogWarning($"[EventSub] Subscription revoked: {subType}");
                break;
            }
        }
    }

    private async Task SubscribeToEventsAsync(string sessionId, CancellationToken ct)
    {
        if (_ChannelId == null) return;

        await SubscribeAsync("channel.follow", "2",
            new { broadcaster_user_id = _ChannelId, moderator_user_id = _ChannelId },
            sessionId, ct);

        await SubscribeAsync("channel.subscribe", "1",
            new { broadcaster_user_id = _ChannelId },
            sessionId, ct);

        await SubscribeAsync("channel.subscription.message", "1",
            new { broadcaster_user_id = _ChannelId },
            sessionId, ct);

        await SubscribeAsync("channel.subscription.gift", "1",
            new { broadcaster_user_id = _ChannelId },
            sessionId, ct);

        await SubscribeAsync("channel.cheer", "1",
            new { broadcaster_user_id = _ChannelId },
            sessionId, ct);

        await SubscribeAsync("channel.channel_points_custom_reward_redemption.add", "1",
            new { broadcaster_user_id = _ChannelId },
            sessionId, ct);
    }

    private async Task SubscribeAsync(string type, string version, object condition, string sessionId, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(new
        {
            type,
            version,
            condition,
            transport = new { method = "websocket", session_id = sessionId }
        });

        StringContent content = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _Http.PostAsync(TwitchConstants.TwitchEventSubSubscriptionsUrl, content, ct);

        if (response.IsSuccessStatusCode)
            _Log.LogWarning($"[EventSub] Subscribed to {type}");
        else
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            _Log.LogError($"[EventSub] Failed to subscribe to {type}: {(int)response.StatusCode} {body}");
        }
    }

    private async Task DispatchEventAsync(string? subType, JsonElement evt, CancellationToken ct)
    {
        switch (subType)
        {
            case "channel.follow":
            {
                string userId = evt.GetProperty("user_id").GetString() ?? "";
                string userName = evt.GetProperty("user_name").GetString() ?? "";
                string followedAtStr = evt.GetProperty("followed_at").GetString() ?? DateTimeOffset.UtcNow.ToString("O");
                DateTimeOffset followedAt = DateTimeOffset.TryParse(followedAtStr, out DateTimeOffset ft) ? ft : DateTimeOffset.UtcNow;

                await RaiseAsync(OnFollow, new FollowHappened(userId, userName, followedAt), ct);
                break;
            }

            case "channel.subscribe":
            {
                bool isGift = evt.TryGetProperty("is_gift", out JsonElement giftProp) && giftProp.GetBoolean();
                if (isGift) break; // handled by channel.subscription.gift

                string userId = evt.GetProperty("user_id").GetString() ?? "";
                string userName = evt.GetProperty("user_name").GetString() ?? "";
                string tier = evt.GetProperty("tier").GetString() ?? "1000";

                await RaiseAsync(OnSub, new SubHappened(
                    Kind: SubKind.New,
                    RecipientUserId: userId,
                    RecipientUserName: userName,
                    GifterUserId: null,
                    GifterUserName: null,
                    Tier: tier,
                    IsPrime: tier == "prime",
                    TotalMonths: null,
                    GiftCount: 0,
                    Message: null,
                    Timestamp: DateTimeOffset.UtcNow
                ), ct);
                break;
            }

            case "channel.subscription.message":
            {
                string userId = evt.GetProperty("user_id").GetString() ?? "";
                string userName = evt.GetProperty("user_name").GetString() ?? "";
                string tier = evt.GetProperty("tier").GetString() ?? "1000";
                int? months = evt.TryGetProperty("cumulative_months", out JsonElement monthsProp)
                    ? monthsProp.GetInt32()
                    : null;
                string? message = evt.TryGetProperty("message", out JsonElement msgProp)
                    && msgProp.TryGetProperty("text", out JsonElement textProp)
                    ? textProp.GetString()
                    : null;

                await RaiseAsync(OnSub, new SubHappened(
                    Kind: SubKind.Resub,
                    RecipientUserId: userId,
                    RecipientUserName: userName,
                    GifterUserId: null,
                    GifterUserName: null,
                    Tier: tier,
                    IsPrime: tier == "prime",
                    TotalMonths: months,
                    GiftCount: 0,
                    Message: message,
                    Timestamp: DateTimeOffset.UtcNow
                ), ct);
                break;
            }

            case "channel.subscription.gift":
            {
                bool isAnon = evt.TryGetProperty("is_anonymous", out JsonElement anonProp) && anonProp.GetBoolean();
                string? gifterId = isAnon ? null : evt.GetProperty("user_id").GetString();
                string? gifterName = isAnon ? null : evt.GetProperty("user_name").GetString();
                string tier = evt.GetProperty("tier").GetString() ?? "1000";
                int total = evt.TryGetProperty("total", out JsonElement totalProp) ? totalProp.GetInt32() : 1;

                await RaiseAsync(OnSub, new SubHappened(
                    Kind: SubKind.CommunityGift,
                    RecipientUserId: "",
                    RecipientUserName: "(community)",
                    GifterUserId: gifterId,
                    GifterUserName: gifterName,
                    Tier: tier,
                    IsPrime: false,
                    TotalMonths: null,
                    GiftCount: total,
                    Message: null,
                    Timestamp: DateTimeOffset.UtcNow
                ), ct);
                break;
            }

            case "channel.cheer":
            {
                bool isAnon = evt.TryGetProperty("is_anonymous", out JsonElement anonProp) && anonProp.GetBoolean();
                string userId = isAnon ? "" : (evt.TryGetProperty("user_id", out JsonElement uidProp) ? uidProp.GetString() ?? "" : "");
                string userName = isAnon ? "" : (evt.TryGetProperty("user_name", out JsonElement unameProp) ? unameProp.GetString() ?? "" : "");
                int bits = evt.TryGetProperty("bits", out JsonElement bitsProp) ? bitsProp.GetInt32() : 0;
                string? message = evt.TryGetProperty("message", out JsonElement msgProp) ? msgProp.GetString() : null;

                await RaiseAsync(OnBits, new BitsHappened(
                    UserId: userId,
                    UserName: userName,
                    IsAnonymous: isAnon,
                    Bits: bits,
                    Message: message,
                    Timestamp: DateTimeOffset.UtcNow
                ), ct);
                break;
            }

            case "channel.channel_points_custom_reward_redemption.add":
            {
                string redemptionId = evt.GetProperty("id").GetString() ?? "";
                string rewardId = evt.GetProperty("reward").GetProperty("id").GetString() ?? "";
                string rewardTitle = evt.GetProperty("reward").GetProperty("title").GetString() ?? "";
                string userId = evt.GetProperty("user_id").GetString() ?? "";
                string userName = evt.GetProperty("user_login").GetString() ?? "";
                string? userInput = evt.TryGetProperty("user_input", out JsonElement inputProp)
                    ? inputProp.GetString()
                    : null;
                string redeemedAtStr = evt.GetProperty("redeemed_at").GetString() ?? DateTimeOffset.UtcNow.ToString("O");
                DateTimeOffset redeemedAt = DateTimeOffset.TryParse(redeemedAtStr, out DateTimeOffset rt) ? rt : DateTimeOffset.UtcNow;

                await RaiseAsync(OnChannelPointRedeemed, new ChannelPointRedeemed(
                    RedemptionId: redemptionId,
                    RewardId: rewardId,
                    RewardTitle: rewardTitle,
                    UserId: userId,
                    UserName: userName,
                    UserInput: userInput,
                    RedeemedAt: redeemedAt
                ), ct);
                break;
            }
        }
    }

    private TimeSpan NextReconnectDelay()
    {
        _ReconnectAttempt = Math.Min(_ReconnectAttempt + 1, 8);
        double baseMs = Math.Pow(2, _ReconnectAttempt) * 250;
        int jitter = Random.Shared.Next(0, 250);
        return TimeSpan.FromMilliseconds(Math.Min(baseMs + jitter, 30_000));
    }

    private static async Task RaiseAsync<T>(Func<T, CancellationToken, Task>? evt, T payload, CancellationToken ct)
    {
        if (evt is null) return;
        foreach (Func<T, CancellationToken, Task> h in evt.GetInvocationList().Cast<Func<T, CancellationToken, Task>>())
            await h(payload, ct);
    }
}
