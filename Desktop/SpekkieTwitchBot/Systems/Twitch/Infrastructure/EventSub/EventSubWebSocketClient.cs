using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.EventSub;

public sealed class EventSubWebSocketClient(Logger logger)
{
    private ClientWebSocket? _Ws;
    private CancellationTokenSource? _Cts;
    private readonly SemaphoreSlim _SendLock = new(1, 1);

    public event Action? OnConnected;
    public event Action<string>? OnMessage;
    public event Action<string?>? OnDisconnected;
    public event Action<Exception>? OnError;

    public async Task ConnectAsync(Uri uri, CancellationToken ct)
    {
        if (_Ws is { State: WebSocketState.Open }) return;

        await CloseAsync(ct).ConfigureAwait(false);

        _Ws = new ClientWebSocket();
        _Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            logger.LogWarning($"[EventSub WS] Connecting to {uri}...");
            await _Ws.ConnectAsync(uri, _Cts.Token).ConfigureAwait(false);

            logger.LogWarning("[EventSub WS] Connected");
            OnConnected?.Invoke();

            _ = Task.Run(() => ReceiveLoop(_Cts.Token), ct);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            await CloseAsync(ct).ConfigureAwait(false);
            OnDisconnected?.Invoke("Connect failed");
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
            StringBuilder sb = new();
            byte[] buffer = new byte[16 * 1024];

            while (!ct.IsCancellationRequested)
            {
                ClientWebSocket? ws = _Ws;
                if (ws is null || ws.State != WebSocketState.Open) break;

                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning($"[EventSub WS] Close frame received. Status={(int?)ws.CloseStatus} Desc={ws.CloseStatusDescription}");
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
}
