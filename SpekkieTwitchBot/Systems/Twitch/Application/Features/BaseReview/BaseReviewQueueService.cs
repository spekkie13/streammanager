using System.Text.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Models.BaseReview;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.BaseReview;

/// <summary>
/// Ordered, persisted queue of base-review requests. Subscribers are placed ahead of
/// non-subscribers (two-tier); within each tier the order is first-come-first-served.
/// The queue is persisted to <c>Output/Twitch/base-review-queue.json</c> so it survives restarts.
/// </summary>
public sealed class BaseReviewQueueService
{
    private static readonly string DefaultFilePath =
        Path.Combine(BotPaths.BaseDir, "Output", "Twitch", "base-review-queue.json");

    private readonly List<BaseReviewEntry> _entries = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Logger _log;
    private readonly string _filePath;

    // filePath is optional so tests can target a temp file; DI honours the default value.
    public BaseReviewQueueService(Logger log, string? filePath = null)
    {
        _log = log;
        _filePath = filePath ?? DefaultFilePath;
        Load();
    }

    /// <summary>Adds an entry respecting two-tier priority. Returns its 1-based position in the queue.</summary>
    public async Task<int> EnqueueAsync(BaseReviewEntry entry, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            int index;
            if (entry.IsSubscriber)
            {
                // Insert right after the last existing subscriber: ahead of all non-subs,
                // behind subscribers who were already waiting.
                index = _entries.FindLastIndex(e => e.IsSubscriber) + 1;
                _entries.Insert(index, entry);
            }
            else
            {
                index = _entries.Count;
                _entries.Add(entry);
            }

            await PersistAsync(ct);
            return index + 1;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Removes and returns the front entry, or null if the queue is empty.</summary>
    public async Task<BaseReviewEntry?> DequeueAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_entries.Count == 0) return null;
            BaseReviewEntry next = _entries[0];
            _entries.RemoveAt(0);
            await PersistAsync(ct);
            return next;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns a snapshot of the current queue in order.</summary>
    public async Task<IReadOnlyList<BaseReviewEntry>> SnapshotAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return _entries.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Empties the queue.</summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _entries.Clear();
            await PersistAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistAsync(CancellationToken ct)
    {
        try
        {
            string json = JsonSerializer.Serialize(_entries, OverlayJson.Options);
            await OverlayJson.WriteAtomicAsync(_filePath, json, ct);
        }
        catch (Exception ex)
        {
            _log.LogError($"[BaseReview] Failed to persist queue: {ex.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            List<BaseReviewEntry>? loaded = JsonSerializer.Deserialize<List<BaseReviewEntry>>(json, OverlayJson.Options);
            if (loaded != null) _entries.AddRange(loaded);
        }
        catch (Exception ex)
        {
            _log.LogError($"[BaseReview] Failed to load queue: {ex.Message}");
        }
    }
}
