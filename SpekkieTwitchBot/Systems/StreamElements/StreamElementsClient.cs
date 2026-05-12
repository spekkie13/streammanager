using System.Text.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.StreamElements;

public sealed class StreamElementsClient
{
    private readonly StreamElementsSocketIoClient _Socket;
    private readonly FileReader _FileReader;
    private readonly Logger _Logger;

    private CancellationToken _RunCt;
    private int _ReconnectAttempt;

    public event Func<DonationHappened, CancellationToken, Task>? OnDonation;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;

    public StreamElementsClient(
        StreamElementsSocketIoClient socket,
        FileReader fileReader,
        Logger logger)
    {
        _Socket = socket;
        _FileReader = fileReader;
        _Logger = logger;

        _Socket.OnSocketConnected += HandleSocketConnected;
        _Socket.OnSocketEvent += HandleSocketEvent;
        _Socket.OnSocketDisconnected += HandleSocketDisconnected;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        _RunCt = ct;
        _ReconnectAttempt = 0;
        await _Socket.ConnectAsync(ct).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        await _Socket.CloseAsync(ct).ConfigureAwait(false);
    }

    private void HandleSocketConnected()
    {
        _ = Task.Run(() => AuthenticateAsync(_RunCt), _RunCt)
            .ContinueWith(
                t => { if (t.Exception != null) _Logger.LogError(t.Exception.ToString()); },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        string jwt = ReadJwtToken();
        if (string.IsNullOrWhiteSpace(jwt))
        {
            _Logger.LogError("[StreamElements] No JWT token in Settings/StreamElements.json — donations will not be tracked");
            return;
        }

        await _Socket.EmitAsync("authenticate", new { method = "jwt", token = jwt }, ct).ConfigureAwait(false);
        _Logger.LogWarning("[StreamElements] Authenticate sent");
    }

    private string ReadJwtToken()
    {
        string path = Path.Combine(BotPaths.BaseDir, "Settings", "StreamElements.json");
        try
        {
            string json = _FileReader.Read(path);
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("JwtToken", out JsonElement prop)
                ? prop.GetString() ?? ""
                : "";
        }
        catch (Exception ex)
        {
            _Logger.LogError($"[StreamElements] Failed to read settings: {ex.Message}");
            return "";
        }
    }

    private void HandleSocketEvent(string eventName, string dataJson)
    {
        _ = Task.Run(() => ProcessEventAsync(eventName, dataJson, _RunCt), _RunCt)
            .ContinueWith(
                t => { if (t.Exception != null) _Logger.LogError(t.Exception.ToString()); },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ProcessEventAsync(string eventName, string dataJson, CancellationToken ct)
    {
        switch (eventName)
        {
            case "authenticated":
                _ReconnectAttempt = 0;
                _Logger.LogWarning("[StreamElements] Authenticated — listening for donations");
                break;

            case "unauthorized":
                _Logger.LogError("[StreamElements] Authentication failed — check JWT token in StreamElements.json");
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

                _Logger.LogInfo($"[StreamElements] Tip received: €{amount} from {username}");

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
        _Logger.LogWarning($"[StreamElements] Disconnected. Reason={reason ?? "unknown"}");

        _ = Task.Run(async () =>
        {
            try
            {
                TimeSpan delay = NextReconnectDelay();
                _Logger.LogWarning($"[StreamElements] Reconnecting in {delay.TotalSeconds:0}s...");
                await Task.Delay(delay, _RunCt).ConfigureAwait(false);
                await _Socket.ConnectAsync(_RunCt).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }, _RunCt);
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
