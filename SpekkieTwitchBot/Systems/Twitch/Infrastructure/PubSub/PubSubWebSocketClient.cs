using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubWebSocketClient
{
    private readonly Logger _log;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _recvLoop;
    private Task? _pingLoop;

    private readonly Uri _uri = new("wss://pubsub-edge.twitch.tv");

    public event Action? OnConnected;
    public event Action<string>? OnMessage;
    public event Action<string?>? OnDisconnected;
    public event Action<Exception>? OnError;

    public PubSubWebSocketClient(Logger log) => _log = log;

    public async Task ConnectAsync(CancellationToken ct)
    {
        if (_ws is { State: WebSocketState.Open }) return;

        await CloseAsync(ct).ConfigureAwait(false);

        _ws = new ClientWebSocket();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            _log.LogWarning("[WS] Connecting...");
            await _ws.ConnectAsync(_uri, _cts.Token).ConfigureAwait(false);

            _log.LogWarning("[WS] Connected");
            OnConnected?.Invoke();

            _recvLoop = Task.Run(() => ReceiveLoop(_cts.Token));
            _pingLoop = Task.Run(() => PingLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            await CloseAsync(ct).ConfigureAwait(false);
            OnDisconnected?.Invoke("Connect failed");
        }
    }

    public void Send(string json)
    {
        var ws = _ws;
        if (ws is null || ws.State != WebSocketState.Open)
            return;

        var bytes = Encoding.UTF8.GetBytes(json);
        _ = ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task CloseAsync(CancellationToken ct)
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }

        if (_ws is null) return;

        try
        {
            if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", ct).ConfigureAwait(false);
        }
        catch { /* ignore */ }

        try { _ws.Dispose(); } catch { /* ignore */ }
        _ws = null;

        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
    }

    public void ForceReconnect(string reason)
    {
        _ = Task.Run(async () =>
        {
            OnDisconnected?.Invoke(reason);
            await CloseAsync(CancellationToken.None).ConfigureAwait(false);
        });
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        try
        {
            var sb = new StringBuilder();
            var buffer = new byte[16 * 1024];

            while (!ct.IsCancellationRequested)
            {
                var ws = _ws;
                if (ws is null || ws.State != WebSocketState.Open) break;

                var result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var desc = ws.CloseStatusDescription;
                    _log.LogWarning($"[WS] Close frame received. CloseStatus={(int?)ws.CloseStatus} Desc={desc}");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;

                var msg = sb.ToString();
                sb.Clear();

                OnMessage?.Invoke(msg);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
        finally
        {
            OnDisconnected?.Invoke("Receive loop ended");
        }
    }

    private async Task PingLoop(CancellationToken ct)
    {
        // Keep session alive; reduces “session unused”.
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(3), ct).ConfigureAwait(false);
            Send("{\"type\":\"PING\"}");
            _log.LogWarning("[WS] PING sent");
        }
    }
}
