using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;

namespace SpekkieTwitchBot.Systems.Twitch;

public sealed class TwitchHostedService : IHostedService
{
    private readonly ITwitchChat _chat;
    private readonly ITwitchEvents _events;
    private readonly TwitchEventRouter _router;
    private readonly Logger _logger;
    private readonly IHostApplicationLifetime _lifetime;
    
    private Task? _runTask;
    private CancellationTokenSource? _cts;
    private int _started;

    public TwitchHostedService(
        ITwitchChat chat,
        ITwitchEvents events,
        TwitchEventRouter router,
        Logger logger,
        IHostApplicationLifetime lifetime,
        TwitchFileSetup twitchFileSetup,
        TimerFileSetup timerFileSetup,
        GeneralFileSetup generalFileSetup
    )
    {
        _chat = chat;
        _events = events;
        _router = router;
        _logger = logger;
        _lifetime = lifetime;
        _ = twitchFileSetup;   // resolved to run file setup on boot
        _ = timerFileSetup;
        _ = generalFileSetup;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {

        // guard: StartAsync kan in rare gevallen 2x worden aangeroepen (of bij retry patterns)
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogInfo("[BOOT] TwitchHostedService starting (logger).");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _router.Wire();
        
        // Belangrijk: log faults van RunAsync, anders lijkt het alsof hij nooit startte
        _runTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);

        _ = _runTask.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Exception? ex = t.Exception?.GetBaseException() ?? t.Exception;
                _logger.LogError("[BOOT] TwitchHostedService RunAsync FAULTED: " + ex);
            }
            else if (t.IsCanceled)
            {
                _logger.LogInfo("[BOOT] TwitchHostedService RunAsync canceled.");
            }
            else
            {
                _logger.LogInfo("[BOOT] TwitchHostedService RunAsync completed.");
            }
        }, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await WithTimeout(token => _chat.ConnectAsync(token), ct, TimeSpan.FromSeconds(15), "chat.ConnectAsync")
                .ConfigureAwait(false);

            await WithTimeout(token => _events.ConnectAsync(token), ct, TimeSpan.FromSeconds(15), "events.ConnectAsync")
                .ConfigureAwait(false);

            await _router.InitializeAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("[BOOT] TwitchHostedService failed: " + ex);
            _lifetime.StopApplication();
        }
        finally
        {
            _logger.LogWarning("TwitchHostedService FINALLY/EXIT");
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInfo("[BOOT] TwitchHostedService stopping.");

        try { _cts?.Cancel(); } catch { /* ignore */ }

        try { _router.Unwire(); } catch { /* ignore */ }

        // Disconnect best-effort (no throw)
        try { await _events.DisconnectAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }
        try { await _chat.DisconnectAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }

        if (_runTask is not null)
        {
            try
            {
                await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(5), ct)).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
        _runTask = null;
    }

    private static async Task WithTimeout(
        Func<CancellationToken, Task> action,
        CancellationToken outerCt,
        TimeSpan timeout,
        string name)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        cts.CancelAfter(timeout);

        Task? op = null;
        try
        {
            op = action(cts.Token);
            Probe.Log($"WithTimeout STARTED: {name}");

            await op.ConfigureAwait(false);

            Probe.Log($"WithTimeout SUCCESS: {name}");
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !outerCt.IsCancellationRequested)
        {
            Probe.Log($"WithTimeout TIMEOUT: {name}");
            throw new TimeoutException($"{name} timed out after {timeout.TotalSeconds:0}s");
        }
        catch (Exception ex)
        {
            Probe.Log($"WithTimeout EXCEPTION: {name} -> {ex}");
            throw;
        }
        finally
        {
            Probe.Log($"WithTimeout EXIT: {name} opStatus={(op is null ? "null" : op.Status.ToString())}");
        }
    }
}
