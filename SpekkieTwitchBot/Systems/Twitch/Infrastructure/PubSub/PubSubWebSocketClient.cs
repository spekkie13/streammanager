using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubWebSocketClient(Logger logger)
{
    private ClientWebSocket? _Ws;
    private CancellationTokenSource? _Cts;
    private readonly SemaphoreSlim _SendLock = new(1, 1);

    private readonly Uri _Uri = new("wss://pubsub-edge.twitch.tv");

    public event Action? OnConnected;
    public event Action<string>? OnMessage;
    public event Action<string?>? OnDisconnected;
    public event Action<Exception>? OnError;
    
    public async Task ConnectAsync(CancellationToken ct)
    {
        if (_Ws is { State: WebSocketState.Open }) return;

        await CloseAsync(ct).ConfigureAwait(false);

        _Ws = new ClientWebSocket();
        _Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            logger.LogWarning("[WS] Connecting...");
            await _Ws.ConnectAsync(_Uri, _Cts.Token).ConfigureAwait(false);

            logger.LogWarning("[WS] Connected");
            OnConnected?.Invoke();

            _ = Task.Run(() => ReceiveLoop(_Cts.Token), ct);
            _ = Task.Run(() => PingLoop(_Cts.Token), ct);
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
        _ = SendAsync(json, CancellationToken.None);
    }

    private async Task SendAsync(string json, CancellationToken ct)
    {
        ClientWebSocket? ws = _Ws;
        if (ws is null || ws.State != WebSocketState.Open)
            return;

        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await _SendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: ct
            ).ConfigureAwait(false);
        }
        finally
        {
            _SendLock.Release();
        }
    }

    public async Task CloseAsync(CancellationToken ct)
    {
        try { _Cts?.Cancel(); } catch { /* ignore */ }

        if (_Ws is null) return;

        try
        {
            if (_Ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await _Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", ct).ConfigureAwait(false);
        }
        catch { /* ignore */ }

        try { _Ws.Dispose(); } catch { /* ignore */ }
        _Ws = null;

        try { _Cts?.Dispose(); } catch { /* ignore */ }
        _Cts = null;
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
            StringBuilder sb = new StringBuilder();
            byte[] buffer = new byte[16 * 1024];

            while (!ct.IsCancellationRequested)
            {
                ClientWebSocket? ws = _Ws;
                if (ws is null || ws.State != WebSocketState.Open) break;

                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    string? desc = ws.CloseStatusDescription;
                    logger.LogWarning($"[WS] Close frame received. CloseStatus={(int?)ws.CloseStatus} Desc={desc}");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;

                string msg = sb.ToString();
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
            await SendAsync("{\"type\":\"PING\"}", ct);
            logger.LogWarning("[WS] PING sent");
        }
    }
}
