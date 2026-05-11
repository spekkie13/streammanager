using System.Text.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.StreamElements;

public sealed class StreamElementsClient
{
    private readonly StreamElementsSocketIoClient _socket;
    private readonly FileReader _fileReader;
    private readonly Logger _logger;

    private CancellationToken _runCt;
    private int _reconnectAttempt;

    public event Func<DonationHappened, CancellationToken, Task>? OnDonation;

    public StreamElementsClient(
        StreamElementsSocketIoClient socket,
        FileReader fileReader,
        Logger logger)
    {
        _socket = socket;
        _fileReader = fileReader;
        _logger = logger;

        _socket.OnSocketConnected += HandleSocketConnected;
        _socket.OnSocketEvent += HandleSocketEvent;
        _socket.OnSocketDisconnected += HandleSocketDisconnected;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        _runCt = ct;
        _reconnectAttempt = 0;
        await _socket.ConnectAsync(ct).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        await _socket.CloseAsync(ct).ConfigureAwait(false);
    }

    private void HandleSocketConnected()
    {
        _ = Task.Run(() => AuthenticateAsync(_runCt), _runCt)
            .ContinueWith(
                t => { if (t.Exception != null) _logger.LogError(t.Exception.ToString()); },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        string jwt = ReadJwtToken();
        if (string.IsNullOrWhiteSpace(jwt))
        {
            _logger.LogError("[StreamElements] No JWT token in Settings/StreamElements.json — donations will not be tracked");
            return;
        }

        await _socket.EmitAsync("authenticate", new { method = "jwt", token = jwt }, ct).ConfigureAwait(false);
        _logger.LogWarning("[StreamElements] Authenticate sent");
    }

    private string ReadJwtToken()
    {
        string path = Path.Combine(BotPaths.BaseDir, "Settings", "StreamElements.json");
        try
        {
            string json = _fileReader.Read(path);
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("JwtToken", out JsonElement prop)
                ? prop.GetString() ?? ""
                : "";
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StreamElements] Failed to read settings: {ex.Message}");
            return "";
        }
    }

    private void HandleSocketEvent(string eventName, string dataJson)
    {
        _ = Task.Run(() => ProcessEventAsync(eventName, dataJson, _runCt), _runCt)
            .ContinueWith(
                t => { if (t.Exception != null) _logger.LogError(t.Exception.ToString()); },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ProcessEventAsync(string eventName, string dataJson, CancellationToken ct)
    {
        switch (eventName)
        {
            case "authenticated":
                _reconnectAttempt = 0;
                _logger.LogWarning("[StreamElements] Authenticated — listening for donations");
                break;

            case "unauthorized":
                _logger.LogError("[StreamElements] Authentication failed — check JWT token in StreamElements.json");
                break;

            case "event":
            {
                using JsonDocument doc = JsonDocument.Parse(dataJson);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("type", out JsonElement typeProp)
                    || typeProp.GetString() != "tip") break;

                if (!root.TryGetProperty("data", out JsonElement data)) break;

                string username = data.TryGetProperty("username", out JsonElement u)
                    ? u.GetString() ?? "anoniem"
                    : "anoniem";

                decimal amount = data.TryGetProperty("amount", out JsonElement a)
                    ? (decimal)a.GetDouble()
                    : 0m;

                string? message = data.TryGetProperty("message", out JsonElement m)
                    ? m.GetString()
                    : null;

                _logger.LogInfo($"[StreamElements] Tip received: €{amount} from {username}");

                await RaiseAsync(OnDonation, new DonationHappened(
                    UserName: username,
                    Amount: amount,
                    Currency: "EUR",
                    Message: message,
                    Timestamp: DateTimeOffset.UtcNow
                ), ct).ConfigureAwait(false);
                break;
            }
        }
    }

    private void HandleSocketDisconnected(string? reason)
    {
        _logger.LogWarning($"[StreamElements] Disconnected. Reason={reason ?? "unknown"}");

        _ = Task.Run(async () =>
        {
            try
            {
                TimeSpan delay = NextReconnectDelay();
                _logger.LogWarning($"[StreamElements] Reconnecting in {delay.TotalSeconds:0}s...");
                await Task.Delay(delay, _runCt).ConfigureAwait(false);
                await _socket.ConnectAsync(_runCt).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }, _runCt);
    }

    private TimeSpan NextReconnectDelay()
    {
        _reconnectAttempt = Math.Min(_reconnectAttempt + 1, 8);
        double baseMs = Math.Pow(2, _reconnectAttempt) * 250;
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
