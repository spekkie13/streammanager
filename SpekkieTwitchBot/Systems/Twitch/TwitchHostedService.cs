using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;

namespace SpekkieTwitchBot.Systems.Twitch;

public sealed class TwitchHostedService : IHostedService
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchEvents _Events;
    private readonly TwitchEventRouter _Router;
    private readonly Logger _Logger;
    private readonly IHostApplicationLifetime _Lifetime;
    
    private Task? _RunTask;
    private CancellationTokenSource? _Cts;
    private int _Started;

    public TwitchHostedService(
        ITwitchChat chat,
        ITwitchEvents events,
        TwitchEventRouter router,
        Logger logger,
        IHostApplicationLifetime lifetime    
    )
    {
        _Chat = chat;
        _Events = events;
        _Router = router;
        _Logger = logger;
        _Lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {

        // guard: StartAsync kan in rare gevallen 2x worden aangeroepen (of bij retry patterns)
        if (Interlocked.Exchange(ref _Started, 1) == 1)
        {
            return Task.CompletedTask;
        }
        
        _Logger.LogInfo("[BOOT] TwitchHostedService starting (logger).");

        _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _Router.Wire();
        
        // Belangrijk: log faults van RunAsync, anders lijkt het alsof hij nooit startte
        _RunTask = Task.Run(() => RunAsync(_Cts.Token), CancellationToken.None);

        _ = _RunTask.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var ex = t.Exception?.GetBaseException() ?? t.Exception;
                _Logger.LogError("[BOOT] TwitchHostedService RunAsync FAULTED: " + ex);
            }
            else if (t.IsCanceled)
            {
                _Logger.LogInfo("[BOOT] TwitchHostedService RunAsync canceled.");
            }
            else
            {
                _Logger.LogInfo("[BOOT] TwitchHostedService RunAsync completed.");
            }
        }, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await WithTimeout(token => _Chat.ConnectAsync(token), ct, TimeSpan.FromSeconds(15), "chat.ConnectAsync")
                .ConfigureAwait(false);

            await WithTimeout(token => _Events.ConnectAsync(token), ct, TimeSpan.FromSeconds(15), "events.ConnectAsync")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _Logger.LogError("[BOOT] TwitchHostedService failed: " + ex);
            _Lifetime.StopApplication();
        }
        finally
        {
            _Logger.LogWarning("TwitchHostedService FINALLY/EXIT");
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _Logger.LogInfo("[BOOT] TwitchHostedService stopping.");

        try { _Cts?.Cancel(); } catch { /* ignore */ }

        try { _Router.Unwire(); } catch { /* ignore */ }

        // Disconnect best-effort (no throw)
        try { await _Events.DisconnectAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }
        try { await _Chat.DisconnectAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }

        if (_RunTask is not null)
        {
            try
            {
                await Task.WhenAny(_RunTask, Task.Delay(TimeSpan.FromSeconds(5), ct)).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        try { _Cts?.Dispose(); } catch { /* ignore */ }
        _Cts = null;
        _RunTask = null;
    }

    private static async Task WithTimeout(
        Func<CancellationToken, Task> action,
        CancellationToken outerCt,
        TimeSpan timeout,
        string name)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
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
