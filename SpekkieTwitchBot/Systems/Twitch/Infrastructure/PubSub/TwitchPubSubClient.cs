using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public class TwitchPubSubClient : ITwitchEvents
{
    private readonly Logger _Log;
    private readonly ITwitchAuthTokenProvider _TokenProvider;
    private readonly PubSubWebSocketClient _Ws;
    private readonly PubSubMessageBuilder _Builder;
    private readonly PubSubMessageParser _Parser;
    private readonly PubSubReconnectPolicy _Reconnect;

    // From files / identity
    private readonly TwitchGeneralFile _Identity;

    // Topics you want (hard requirements)
    private readonly string[] _Topics;

    private CancellationToken _RunCt;
    private bool _ListenSent;
    private DateTimeOffset _LastInboundUtc = DateTimeOffset.MinValue;
    
    public event Func<FollowHappened, CancellationToken, Task>? OnFollow;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;
    public event Func<ChannelPointRedeemed, CancellationToken, Task>? OnChannelPointRedeemed;
    
    public TwitchPubSubClient(
        Logger log,
        ITwitchAuthTokenProvider tokenProvider,
        TwitchFileReader twitchFileReader,
        PubSubWebSocketClient ws,
        PubSubMessageBuilder builder,
        PubSubMessageParser parser,
        PubSubReconnectPolicy reconnect)
    {
        _Log = log;
        _TokenProvider = tokenProvider;
        _Ws = ws;
        _Builder = builder;
        _Parser = parser;
        _Reconnect = reconnect;

        var identityJson = twitchFileReader.ReadTwitchGeneralAuthFile();
        _Identity = JsonConvert.DeserializeObject<TwitchGeneralFile>(identityJson) 
                    ?? throw new InvalidOperationException("TwitchGeneralFile missing/invalid");

        if (string.IsNullOrWhiteSpace(_Identity.ChannelId))
            throw new InvalidOperationException("ChannelId is empty in TwitchGeneralFile");

        // ✅ Your 4 topics as “hard requirements”
        _Topics =
        [
            $"following.{_Identity.ChannelId}",
            $"channel-subscribe-events-v1.{_Identity.ChannelId}",
            $"channel-points-channel-v1.{_Identity.ChannelId}",
            $"community-points-channel-v1.{_Identity.ChannelId}"
        ];

        // Wire transport callbacks
        _Ws.OnConnected += HandleConnected;
        _Ws.OnMessage += HandleMessage;
        _Ws.OnDisconnected += HandleDisconnected;
        _Ws.OnError += ex => _Log.LogError($"[PubSub] WS error: {ex}");
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _RunCt = cancellationToken;
        _ListenSent = false;

        _Log.LogWarning("[PubSub] ConnectAsync starting");
        await _Ws.ConnectAsync(_RunCt);

        // keepalive + health: start ping loop in ws or here (shown in ws section)
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _Log.LogWarning("[PubSub] DisconnectAsync");
        await _Ws.CloseAsync(cancellationToken);
    }

    private async void HandleConnected()
    {
        try
        {
            _Log.LogWarning("[PubSub] Connected -> sending LISTEN");

            // IMPORTANT: use a USER access token for PubSub LISTEN (not app token)
            var userToken = await _TokenProvider.GetUserAccessTokenAsync(_RunCt);
            if (string.IsNullOrWhiteSpace(userToken))
            {
                _Log.LogError("[PubSub] User token empty -> cannot LISTEN.");
                return;
            }

            var listenJson = _Builder.BuildListen(topics: _Topics, userAccessToken: userToken);
            _Ws.Send(listenJson);

            _ListenSent = true;
            _Log.LogWarning($"[PubSub] LISTEN sent topics={_Topics.Length}");
        }
        catch (Exception ex)
        {
            _Log.LogError($"[PubSub] HandleConnected failed: {ex}");
        }
    }

    private void HandleDisconnected(string? reason)
    {
        _Log.LogWarning($"[PubSub] Disconnected. Reason={reason ?? "unknown"}");

        // If Twitch says “session unused”, it usually means no LISTEN or no activity.
        // We already send LISTEN on connect. Reconnect with backoff:
        _ = Task.Run(async () =>
        {
            var delay = _Reconnect.NextDelay();
            _Log.LogWarning($"[PubSub] Reconnect scheduled in {delay.TotalMilliseconds:0}ms");
            await Task.Delay(delay, _RunCt).ConfigureAwait(false);
            await _Ws.ConnectAsync(_RunCt).ConfigureAwait(false);
        });
    }

    private void HandleMessage(string raw)
    {
        _LastInboundUtc = DateTimeOffset.UtcNow;

        _Log.LogWarning(raw);
        var msg = _Parser.Parse(raw);
        switch (msg.Kind)
        {
            case PubSubInboundKind.Response:
                _Log.LogWarning($"[PubSub][RESPONSE] nonce={msg.Nonce} ok={msg.Success} error={msg.Error}");
                // If BadAuth -> your token flow is broken (wrong token type/expired/missing scopes)
                return;

            case PubSubInboundKind.Pong:
                _Log.LogWarning("[PubSub][PONG]");
                return;

            case PubSubInboundKind.Reconnect:
                _Log.LogWarning("[PubSub][RECONNECT] server requested");
                _Ws.ForceReconnect("Server RECONNECT");
                return;

            case PubSubInboundKind.Message:
                DispatchDomainEvent(msg);
                return;

            default:
                _Log.LogWarning($"[PubSub] Unhandled inbound: {raw}");
                return;
        }
    }

    private void DispatchDomainEvent(PubSubInboundMessage msg)
    {
        // Keep it dumb first: only implement the 3 events you need.
        // Later you can add topic-based dispatch.

        if (msg.Topic.StartsWith("following."))
        {
            var model = new FollowHappened(
                UserId: msg.UserId ?? "",
                UserName: msg.UserName ?? "",
                FollowedAt: DateTimeOffset.UtcNow
            );
            FireAndForget(RaiseAsync(OnFollow, model, _RunCt));
            return;
        }

        if (msg.Topic.StartsWith("channel-subscribe-events-v1."))
        {
            if (msg.Sub is null) return;
            FireAndForget(RaiseAsync(OnSub, msg.Sub, _RunCt));
            return;
        }

        if (msg.Topic.StartsWith("channel-points-channel-v1.") || msg.Topic.StartsWith("community-points-channel-v1."))
        {
            if (msg.Redemption is null) return;
            FireAndForget(RaiseAsync(OnChannelPointRedeemed, msg.Redemption, _RunCt));
            return;
        }
    }

    private static async Task RaiseAsync<T>(Func<T, CancellationToken, Task>? evt, T payload, CancellationToken ct)
    {
        if (evt is null) return;
        foreach (var h in evt.GetInvocationList().Cast<Func<T, CancellationToken, Task>>())
            await h(payload, ct);
    }

    private void FireAndForget(Task task)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.Exception != null)
                _Log.LogError(t.Exception.ToString());
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}