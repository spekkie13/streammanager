using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class TimedMessagesFeature(ITwitchChat chat) : IDisposable
{
    private static readonly string ConfigPath =
        Path.Combine(BotPaths.BaseDir, "Settings", "TimedMessages.json");

    private FileSystemWatcher? _Watcher;
    private CancellationTokenSource? _WatcherDebounce;
    private CancellationTokenSource? _LoopsCts;
    private CancellationToken _StopToken;

    public Task InitializeAsync(CancellationToken ct)
    {
        _StopToken = ct;
        StartLoops();
        StartWatcher();
        return Task.CompletedTask;
    }

    private void StartLoops()
    {
        _LoopsCts?.Cancel();
        _LoopsCts?.Dispose();
        _LoopsCts = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        CancellationToken loopToken = _LoopsCts.Token;

        List<TimedMessage> messages = LoadTimedMessages();
        foreach (TimedMessage entry in messages)
        {
            if (entry.Message is null || entry.IntervalMinutes <= 0) continue;
            _ = RunLoopAsync(entry.Message, entry.IntervalMinutes, loopToken);
        }
    }

    private void StartWatcher()
    {
        string dir = Path.GetDirectoryName(ConfigPath)!;
        string file = Path.GetFileName(ConfigPath);

        _Watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _Watcher.Changed += OnConfigChanged;
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        CancellationTokenSource? oldCts = Interlocked.Exchange(ref _WatcherDebounce, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, newCts.Token);
                StartLoops();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[TimedMessages] Error reloading config: {ex}");
            }
        }, newCts.Token);
    }

    private async Task RunLoopAsync(string message, int intervalMinutes, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), ct);
                await chat.SendAsync(message, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[TimedMessages] Error sending message: {ex}");
        }
    }

    private static List<TimedMessage> LoadTimedMessages()
    {
        using FileStream rfs = new(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader sr = new(rfs);
        string json = sr.ReadToEnd();
        TimedMessages config = JsonConvert.DeserializeObject<TimedMessages>(json) ?? new TimedMessages();
        return config.Messages;
    }

    public void Dispose()
    {
        _Watcher?.Dispose();
        _WatcherDebounce?.Cancel();
        _WatcherDebounce?.Dispose();
        _LoopsCts?.Cancel();
        _LoopsCts?.Dispose();
    }
}