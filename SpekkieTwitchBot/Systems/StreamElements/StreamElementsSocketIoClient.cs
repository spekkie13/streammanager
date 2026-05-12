using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.Systems.StreamElements;

// Socket.IO v2 (EIO=3) transport layer.
// Handles EIO ping/pong, message framing, and Socket.IO event parsing.
public sealed class StreamElementsSocketIoClient(Logger logger)
{
    private static readonly Uri RealtimeUri = new(
        "wss://realtime.streamelements.com/socket.io/?EIO=3&transport=websocket");

    private ClientWebSocket? _Ws;
    private CancellationTokenSource? _Cts;
    private readonly SemaphoreSlim _SendLock = new(1, 1);

    public event Action? OnSocketConnected;
    public event Action<string, string>? OnSocketEvent; // (eventName, dataJson)
    public event Action<string?>? OnSocketDisconnected;

    public async Task ConnectAsync(CancellationToken ct)
    {
        await CloseAsync(ct).ConfigureAwait(false);

        _Ws = new ClientWebSocket();
        _Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            logger.LogWarning("[SE WS] Connecting...");
            await _Ws.ConnectAsync(RealtimeUri, _Cts.Token).ConfigureAwait(false);
            _ = Task.Run(() => ReceiveLoop(_Cts.Token), _Cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError($"[SE WS] Connect failed: {ex.Message}");
            OnSocketDisconnected?.Invoke("Connect failed");
        }
    }

    public async Task EmitAsync(string eventName, object data, CancellationToken ct)
    {
        string payload = JsonSerializer.Serialize(new[] { eventName, data });
        await SendRawAsync($"42{payload}", ct).ConfigureAwait(false);
    }

    public async Task CloseAsync(CancellationToken ct)
    {
        try { _Cts?.Cancel(); } catch { /* ignore */ }

        if (_Ws is not null)
        {
            try
            {
                if (_Ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await _Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", ct).ConfigureAwait(false);
            }
            catch { /* ignore */ }
            try { _Ws.Dispose(); } catch { /* ignore */ }
            _Ws = null;
        }

        try { _Cts?.Dispose(); } catch { /* ignore */ }
        _Cts = null;
    }

    private async Task SendRawAsync(string message, CancellationToken ct)
    {
        if (_Ws is not { State: WebSocketState.Open }) return;

        await _SendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await _Ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }
        finally
        {
            _SendLock.Release();
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        try
        {
            StringBuilder sb = new();
            byte[] buffer = new byte[32 * 1024];

            while (!ct.IsCancellationRequested)
            {
                if (_Ws is not { State: WebSocketState.Open }) break;

                WebSocketReceiveResult result = await _Ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning("[SE WS] Close frame received");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;

                string msg = sb.ToString();
                sb.Clear();

                HandleEioPacket(msg, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"[SE WS] Receive error: {ex.Message}");
        }
        finally
        {
            OnSocketDisconnected?.Invoke("Receive loop ended");
        }
    }

    private void HandleEioPacket(string packet, CancellationToken ct)
    {
        if (packet.Length == 0) return;

        switch (packet[0])
        {
            case '0': // EIO open — start ping loop
            {
                int pingInterval = 25_000;
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(packet.Substring(1));
                    if (doc.RootElement.TryGetProperty("pingInterval", out JsonElement p))
                        pingInterval = p.GetInt32();
                }
                catch { /* use default */ }

                _ = Task.Run(() => PingLoop(pingInterval, ct), ct);
                break;
            }

            case '3': // EIO pong — nothing to do
                break;

            case '4' when packet.Length >= 2:
            {
                switch (packet[1])
                {
                    case '0': // SIO connect
                        logger.LogWarning("[SE WS] Socket.IO connected");
                        OnSocketConnected?.Invoke();
                        break;

                    case '2' when packet.Length > 2: // SIO event: 42[name, data]
                    {
                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(packet.Substring(2));
                            JsonElement root = doc.RootElement;
                            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2) break;

                            string eventName = root[0].GetString() ?? "";
                            string dataJson = root[1].GetRawText();
                            OnSocketEvent?.Invoke(eventName, dataJson);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"[SE WS] Failed to parse event: {ex.Message}");
                        }
                        break;
                    }
                }
                break;
            }
        }
    }

    private async Task PingLoop(int intervalMs, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
                await SendRawAsync("2", ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"[SE WS] Ping loop error: {ex.Message}");
        }
    }
}
