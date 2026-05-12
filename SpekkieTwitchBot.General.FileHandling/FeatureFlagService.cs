using System.Text.Json;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling;

public sealed class FeatureFlagService(Logger logger, string? flagsPath = null) : IFeatureFlagService, IDisposable
{
    private readonly string FlagsPath =
        flagsPath ?? Path.Combine(BotPaths.BaseDir, "Settings", "features.json");

    private IReadOnlyDictionary<string, bool> _Flags = new Dictionary<string, bool>();
    private FileSystemWatcher? _Watcher;
    private CancellationTokenSource? _Debounce;
    private CancellationToken _StopToken;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _StopToken = cancellationToken;
        await ReloadAsync();
        StartWatcher();
    }

    public bool IsEnabled(string flag) =>
        _Flags.TryGetValue(flag, out bool enabled) && enabled;

    private void StartWatcher()
    {
        string dir = Path.GetDirectoryName(FlagsPath)!;
        if (!Directory.Exists(dir)) return;

        _Watcher = new FileSystemWatcher(dir, Path.GetFileName(FlagsPath))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _Watcher.Changed += OnChanged;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(_StopToken);
        CancellationTokenSource? oldCts = Interlocked.Exchange(ref _Debounce, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, newCts.Token);
                await ReloadAsync();
                logger.LogInfo("[FeatureFlags] Reloaded features.json");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"[FeatureFlags] Reload failed: {ex.Message}");
            }
        }, newCts.Token);
    }

    private async Task ReloadAsync()
    {
        if (!File.Exists(FlagsPath)) return;

        try
        {
            string json = await File.ReadAllTextAsync(FlagsPath);
            Dictionary<string, bool>? parsed =
                JsonSerializer.Deserialize<Dictionary<string, bool>>(json);

            if (parsed != null)
                Interlocked.Exchange(ref _Flags, parsed);
        }
        catch (Exception ex)
        {
            logger.LogError($"[FeatureFlags] Invalid features.json: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _Watcher?.Dispose();
        _Debounce?.Cancel();
        _Debounce?.Dispose();
    }
}
