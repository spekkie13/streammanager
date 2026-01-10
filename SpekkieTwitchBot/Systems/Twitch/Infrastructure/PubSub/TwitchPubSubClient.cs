using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public class TwitchPubSubClient : ITwitchEvents
{
    private readonly Logger _Log;
    private readonly ITwitchAuthTokenProvider _TokenProvider;
    private readonly PubSubWebSocketClient _Ws;
    private readonly PubSubReconnectPolicy _Reconnect;

    private readonly string[] _Topics;

    private CancellationToken _RunCt;
    private bool _ListenSent;
    
    public event Func<FollowHappened, CancellationToken, Task>? OnFollow;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;
    public event Func<ChannelPointRedeemed, CancellationToken, Task>? OnChannelPointRedeemed;
    
    public TwitchPubSubClient(
        Logger log,
        ITwitchAuthTokenProvider tokenProvider,
        TwitchFileReader twitchFileReader,
        PubSubWebSocketClient ws,
        PubSubReconnectPolicy reconnect)
    {
        _Log = log;
        _TokenProvider = tokenProvider;
        _Ws = ws;
        _Reconnect = reconnect;

        string identityJson = twitchFileReader.ReadTwitchGeneralAuthFile();
        TwitchGeneralFile identity = JsonConvert.DeserializeObject<TwitchGeneralFile>(identityJson) 
                       ?? throw new InvalidOperationException("TwitchGeneralFile missing/invalid");

        if (string.IsNullOrWhiteSpace(identity.ChannelId))
            throw new InvalidOperationException("ChannelId is empty in TwitchGeneralFile");

        _Topics =
        [
            $"following.{identity.ChannelId}",
            $"channel-subscribe-events-v1.{identity.ChannelId}",
            $"channel-points-channel-v1.{identity.ChannelId}",
            $"community-points-channel-v1.{identity.ChannelId}"
        ];

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
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _Log.LogWarning("[PubSub] DisconnectAsync");
        await _Ws.CloseAsync(cancellationToken);
    }

    private void HandleConnected()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (_ListenSent) return;
                _Log.LogWarning("[PubSub] Connected -> sending LISTEN");

                string userToken = await _TokenProvider.GetUserAccessTokenAsync(_RunCt);
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    _Log.LogError("[PubSub] User token empty -> cannot LISTEN.");
                    return;
                }

                string listenJson = PubSubMessageBuilder.BuildListen(topics: _Topics, userAccessToken: userToken);
                _Ws.Send(listenJson);

                _ListenSent = true;
                _Log.LogWarning($"[PubSub] LISTEN sent topics={_Topics.Length}");
            }
            catch (Exception ex)
            {
                _Log.LogError($"[PubSub] HandleConnected failed: {ex}");
            }
        }, _RunCt);
    }

    private void HandleDisconnected(string? reason)
    {
        _Log.LogWarning($"[PubSub] Disconnected. Reason={reason ?? "unknown"}");
        _ListenSent = false;
        
        _ = Task.Run(async () =>
        {
            TimeSpan delay = _Reconnect.NextDelay();
            _Log.LogWarning($"[PubSub] Reconnect scheduled in {delay.TotalMilliseconds:0}ms");
            await Task.Delay(delay, _RunCt).ConfigureAwait(false);
            await _Ws.ConnectAsync(_RunCt).ConfigureAwait(false);
        }, _RunCt);
    }

    private void HandleMessage(string raw)
    {
        PubSubInboundMessage msg = PubSubMessageParser.Parse(raw);
        switch (msg.Kind)
        {
            case PubSubInboundKind.Response:
                _Log.LogWarning($"[PubSub][RESPONSE] nonce={msg.Nonce} ok={msg.Success} error={msg.Error}");
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

            case PubSubInboundKind.Unknown:
            default:
                _Log.LogWarning($"[PubSub] Unhandled inbound: {raw}");
                return;
        }
    }

    private void DispatchDomainEvent(PubSubInboundMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Topic)) return;
        
        if (msg.Topic.StartsWith("following."))
        {
            FollowHappened model = new FollowHappened(
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
        }
    }

    private static async Task RaiseAsync<T>(Func<T, CancellationToken, Task>? evt, T payload, CancellationToken ct)
    {
        if (evt is null) return;
        foreach (Func<T, CancellationToken, Task> h in evt.GetInvocationList().Cast<Func<T, CancellationToken, Task>>())
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