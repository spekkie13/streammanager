using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;

public sealed class TwitchIrcWebSocketTransport
{
    private readonly Uri _Uri = new("wss://irc-ws.chat.twitch.tv:443");
    private readonly Logger _Log;

    private readonly object _Gate = new();

    private ClientWebSocket? _Ws;
    private CancellationTokenSource? _RunCts;
    private Task? _RecvTask;

    private int _DisconnectFired;

    public event Action? OnConnected;
    public event Action<string?>? OnDisconnected;
    public event Action<Exception>? OnError;

    public event Func<string, Task>? OnRawLine;

    public bool AwaitRawLineHandlers { get; set; } = false;

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TwitchIrcWebSocketTransport(Logger log)
    {
        _Log = log;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        // quick check
        var existing = _Ws;
        if (existing is { State: WebSocketState.Open })
        {
            _Log.LogWarning($"[IRC-WS] ConnectAsync called but already connected. State={existing.State}");
            return;
        }
        
        // Cleanup previous session first (NO SAME ct)
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

        // new session state
        var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        // linked token: stop host => stop loop; plus connect timeout
        var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(runCts.Token);
        connectCts.CancelAfter(ConnectTimeout);

        lock (_Gate)
        {
            _Ws = ws;
            _RunCts = runCts;
            _RecvTask = null;
            _DisconnectFired = 0;
        }

        try
        {
            await ws.ConnectAsync(_Uri, connectCts.Token).ConfigureAwait(false);

            OnConnected?.Invoke();

            // start receive loop
            var task = Task.Run(() => ReceiveLoop(runCts.Token), CancellationToken.None);
            lock (_Gate) _RecvTask = task;

            // log completion/faults
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _Log.LogError("[IRC-WS] ReceiveLoop faulted: " + t.Exception);
                    OnError?.Invoke(t.Exception!);
                }
                else if (t.IsCanceled)
                {
                    _Log.LogWarning("[IRC-WS] ReceiveLoop canceled");
                }
                else
                {
                    _Log.LogWarning("[IRC-WS] ReceiveLoop completed");
                }
            }, TaskScheduler.Default);
        }
        catch (OperationCanceledException oce) when (connectCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // connect timeout
            _Log.LogError($"[IRC-WS] ConnectAsync timed out after {ConnectTimeout.TotalSeconds:0}s");
            OnError?.Invoke(oce);

            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            FireDisconnectedOnce("Connect timeout");
        }
        catch (Exception ex)
        {
            _Log.LogError("[IRC-WS] ConnectAsync failed: " + ex);
            OnError?.Invoke(ex);

            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            FireDisconnectedOnce("Connect failed");
        }
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        ClientWebSocket? ws;
        CancellationTokenSource? runCts;
        Task? recvTask;

        lock (_Gate)
        {
            ws = _Ws;
            runCts = _RunCts;
            recvTask = _RecvTask;

            _Ws = null;
            _RunCts = null;
            _RecvTask = null;
        }

        try { runCts?.Cancel(); } catch { /* ignore */ }

        // wait for receive loop to exit (best-effort)
        if (recvTask is not null)
        {
            try
            {
                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                waitCts.CancelAfter(DisconnectTimeout);

                _Log.LogWarning($"[IRC-WS] Waiting for ReceiveLoop (timeout={DisconnectTimeout.TotalSeconds:0}s) ...");
                await recvTask.WaitAsync(waitCts.Token).ConfigureAwait(false);
                _Log.LogWarning("[IRC-WS] ReceiveLoop stopped");
            }
            catch (OperationCanceledException)
            {
                _Log.LogWarning("[IRC-WS] ReceiveLoop wait timed out (ignored)");
            }
            catch (Exception ex)
            {
                _Log.LogError("[IRC-WS] ReceiveLoop wait error (ignored): " + ex);
            }
        }

        if (ws is null)
        {
            _Log.LogWarning("[IRC-WS] DisconnectAsync: ws already null");
            try { runCts?.Dispose(); } catch { /* ignore */ }
            _Log.LogWarning("[IRC-WS] DisconnectAsync done");
            return;
        }

        try
        {
            _Log.LogWarning($"[IRC-WS] DisconnectAsync: state={ws.State}");
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct).ConfigureAwait(false);
                _Log.LogWarning("[IRC-WS] CloseAsync done");
            }
        }
        catch (Exception ex)
        {
            _Log.LogError("[IRC-WS] CloseAsync failed (ignored): " + ex.Message);
        }

        try { ws.Dispose(); } catch { /* ignore */ }
        try { runCts?.Dispose(); } catch { /* ignore */ }

        _Log.LogWarning("[IRC-WS] DisconnectAsync done");
    }

    public async Task SendRawAsync(string line, CancellationToken ct)
    {
        var ws = _Ws;
        if (ws is null)
        {
            _Log.LogError("[IRC-WS] SEND failed: ws is null");
            throw new InvalidOperationException("IRC transport is not connected (ws null).");
        }

        if (ws.State != WebSocketState.Open)
        {
            _Log.LogError($"[IRC-WS] SEND failed: ws not open (state={ws.State})");
            throw new InvalidOperationException($"IRC transport is not connected (state={ws.State}).");
        }

        // Never log oauth/tokens in plaintext
        string preview = SanitizeForLog(line);
        _Log.LogWarning($"[IRC-WS] SEND: {Trunc(preview, 200)}");

        byte[] bytes = Encoding.UTF8.GetBytes(line + "\r\n");
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        _Log.LogWarning("[IRC-WS] ReceiveLoop begin");

        byte[] buffer = new byte[16 * 1024];
        var sb = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var ws = _Ws;
                if (ws is null)
                {
                    _Log.LogWarning("[IRC-WS] ReceiveLoop break: ws null");
                    break;
                }

                if (ws.State != WebSocketState.Open)
                {
                    _Log.LogWarning($"[IRC-WS] ReceiveLoop break: ws state={ws.State}");
                    break;
                }

                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    string? desc = ws.CloseStatusDescription;
                    _Log.LogWarning($"[IRC-WS] Close frame received. CloseStatus={(int?)ws.CloseStatus} Desc={desc}");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    _Log.LogWarning($"[IRC-WS] Ignoring non-text frame: {result.MessageType}");
                    continue;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;

                string raw = sb.ToString();
                sb.Clear();

                foreach (string line in raw.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    // beperkte debug log
                    if (line.StartsWith("PING", StringComparison.Ordinal) ||
                        line.Contains("PRIVMSG", StringComparison.Ordinal) ||
                        line.Contains("NOTICE", StringComparison.Ordinal))
                    {
                        _Log.LogWarning($"[IRC-WS] RECV: {Trunc(SanitizeForLog(line), 200)}");
                    }

                    var handler = OnRawLine;
                    if (handler is null) continue;

                    if (AwaitRawLineHandlers)
                        await SafeInvoke(() => handler(line)).ConfigureAwait(false);
                    else
                        _ = SafeInvoke(() => handler(line));
                }
            }
        }
        catch (OperationCanceledException)
        {
            _Log.LogWarning("[IRC-WS] ReceiveLoop canceled");
        }
        catch (Exception ex)
        {
            _Log.LogError("[IRC-WS] ReceiveLoop error: " + ex);
            OnError?.Invoke(ex);
        }
        finally
        {
            _Log.LogWarning("[IRC-WS] ReceiveLoop ended -> OnDisconnected");
            FireDisconnectedOnce("Receive loop ended");
        }
    }

    private async Task SafeInvoke(Func<Task> handler)
    {
        try { await handler().ConfigureAwait(false); }
        catch (Exception ex)
        {
            _Log.LogError("[IRC-WS] Handler error: " + ex);
            OnError?.Invoke(ex);
        }
    }

    private void FireDisconnectedOnce(string? reason)
    {
        if (Interlocked.Exchange(ref _DisconnectFired, 1) != 0)
            return;

        try { OnDisconnected?.Invoke(reason); }
        catch (Exception ex) { _Log.LogError("[IRC-WS] OnDisconnected handler threw: " + ex); }
    }

    private static string Trunc(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";

    private static string SanitizeForLog(string line)
    {
        if (line.StartsWith("PASS oauth:", StringComparison.OrdinalIgnoreCase))
            return "PASS oauth:***";
        return line;
    }
}
