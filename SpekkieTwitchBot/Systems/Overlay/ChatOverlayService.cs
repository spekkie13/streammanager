using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Overlay;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Maintains a small ring buffer of the most recent chat messages and writes it to
/// <c>Output/chat-state.json</c> on its own ~1s cadence (decoupled from the slower
/// overlay-state.json). The overlay polls the file the same way as the other state files.
/// </summary>
public sealed class ChatOverlayService(
    ITwitchAuthTokenProvider tokens,
    Logger logger)
    : BackgroundService
{
    private const int MaxMessages = 25;
    private static readonly TimeSpan WriteInterval = TimeSpan.FromSeconds(1);

    // Third-party chat bots whose messages should not appear on the overlay.
    private static readonly HashSet<string> BotDenylist = new(StringComparer.OrdinalIgnoreCase)
    {
        "nightbot", "streamelements", "streamlabs", "moobot", "fossabot", "soundalert", "sery_bot"
    };

    private static readonly string OutputPath =
        Path.Combine(BotPaths.BaseDir, "Output", "chat-state.json");

    private static readonly string OverlayHtmlPath =
        Path.Combine(BotPaths.BaseDir, "Output", "chat-overlay.html");

    private readonly Queue<ChatOverlayMessage> _buffer = new(MaxMessages);
    private readonly object _gate = new();
    private bool _dirty;
    private string _botName = "";

    /// <summary>Called synchronously from the IRC receive path; keep it cheap and allocation-light.</summary>
    public void Append(ChatMessageReceived e)
    {
        if (!ShouldInclude(e, _botName)) return;

        ChatOverlayMessage msg = new()
        {
            Id = e.MessageId,
            User = e.Username,
            Text = e.Text,
            At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        };

        lock (_gate)
        {
            if (_buffer.Count >= MaxMessages) _buffer.Dequeue();
            _buffer.Enqueue(msg);
            _dirty = true;
        }
    }

    /// <summary>Filters out commands, the bot's own messages, and known third-party bots.</summary>
    public static bool ShouldInclude(ChatMessageReceived e, string botName)
    {
        if (string.IsNullOrWhiteSpace(e.Text) || e.Text.StartsWith('!')) return false;
        if (!string.IsNullOrEmpty(botName) && string.Equals(e.Username, botName, StringComparison.OrdinalIgnoreCase)) return false;
        if (BotDenylist.Contains(e.Username)) return false;
        return true;
    }

    /// <summary>Current buffer contents, oldest first. Primarily for tests.</summary>
    public IReadOnlyList<ChatOverlayMessage> Snapshot()
    {
        lock (_gate) return _buffer.ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EnsureOverlayHtml();

        try
        {
            var identity = await tokens.ReadIdentityAsync(stoppingToken);
            _botName = (identity.BotName ?? identity.BroadcasterName ?? "").Trim();
        }
        catch (Exception ex)
        {
            logger.LogError($"[ChatOverlay] Failed to read bot identity: {ex.Message}");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await WriteIfDirtyAsync(stoppingToken);
            await Task.Delay(WriteInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    // Ship the bundled browser-source overlay next to its data file so OBS can point at it directly.
    private void EnsureOverlayHtml()
    {
        try
        {
            string? dir = Path.GetDirectoryName(OverlayHtmlPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(OverlayHtmlPath, ChatOverlayHtml.Content);
        }
        catch (Exception ex)
        {
            logger.LogError($"[ChatOverlay] Failed to write overlay HTML: {ex.Message}");
        }
    }

    private async Task WriteIfDirtyAsync(CancellationToken ct)
    {
        ChatOverlayState? state = null;
        lock (_gate)
        {
            if (_dirty)
            {
                state = new ChatOverlayState
                {
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    Messages = _buffer.ToList()
                };
                _dirty = false;
            }
        }

        if (state == null) return;

        try
        {
            string json = JsonSerializer.Serialize(state, OverlayJson.Options);
            await OverlayJson.WriteAtomicAsync(OutputPath, json, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Re-flag so the next tick retries the write.
            lock (_gate) _dirty = true;
            logger.LogError($"[ChatOverlay] Error writing chat state: {ex.Message}");
        }
    }
}
