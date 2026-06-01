using System.Globalization;
using System.Text;
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

    // /me messages arrive wrapped as <0x01>ACTION <text><0x01>.
    private const char ActionMarker = (char)1;
    private static readonly string ActionPrefix = ActionMarker + "ACTION ";

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

        (string text, bool isAction) = StripAction(e.Text);
        List<string> badges = ParseBadges(e.Badges);

        ChatOverlayMessage msg = new()
        {
            Id = e.MessageId,
            User = e.Username,
            Login = e.Login,
            UserId = e.UserId,
            Text = text,
            Segments = BuildSegments(text, e.Emotes),
            At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            Color = e.Color,
            Badges = badges,
            IsBroadcaster = HasBadge(badges, "broadcaster"),
            IsMod = HasBadge(badges, "moderator"),
            IsVip = HasBadge(badges, "vip"),
            IsSubscriber = HasBadge(badges, "subscriber") || HasBadge(badges, "founder"),
            IsAction = isAction,
            IsFirstMessage = e.IsFirstMessage,
            IsHighlighted = e.IsHighlighted,
            Bits = e.Bits,
            ReplyParent = string.IsNullOrEmpty(e.ReplyParentDisplayName)
                ? null
                : new ChatReplyParent { User = e.ReplyParentDisplayName, Text = e.ReplyParentBody }
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

        string key = string.IsNullOrEmpty(e.Login) ? e.Username : e.Login;
        if (!string.IsNullOrEmpty(botName) && string.Equals(key, botName, StringComparison.OrdinalIgnoreCase)) return false;
        if (BotDenylist.Contains(key)) return false;
        return true;
    }

    private static (string text, bool isAction) StripAction(string text)
    {
        if (text.Length > ActionPrefix.Length && text[^1] == ActionMarker &&
            text.StartsWith(ActionPrefix, StringComparison.Ordinal))
            return (text.Substring(ActionPrefix.Length, text.Length - ActionPrefix.Length - 1), true);
        return (text, false);
    }

    // Badges tag is "name/version,name/version"; keep the ordered tokens for the overlay.
    private static List<string> ParseBadges(string badges) =>
        string.IsNullOrEmpty(badges)
            ? []
            : badges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static bool HasBadge(List<string> badges, string name) =>
        badges.Any(b => b.StartsWith(name + "/", StringComparison.OrdinalIgnoreCase) ||
                        b.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Splits message text into text/emote runs from the IRC emotes tag
    /// ("id:start-end,start-end/id2:start-end"). Positions are Unicode codepoint indices, so we
    /// index by <see cref="Rune"/> rather than UTF-16 char. Returns [] when there are no emotes;
    /// out-of-range positions (e.g. /me offset quirks) are skipped so the text still renders.
    /// </summary>
    public static List<ChatMessageSegment> BuildSegments(string text, string emotesTag)
    {
        if (string.IsNullOrEmpty(emotesTag)) return [];

        List<(int start, int end, string id)> ranges = [];
        foreach (string emote in emotesTag.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] idAndPositions = emote.Split(':', 2);
            if (idAndPositions.Length != 2) continue;

            string id = idAndPositions[0];
            foreach (string pos in idAndPositions[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] se = pos.Split('-', 2);
                if (se.Length == 2 && int.TryParse(se[0], out int s) && int.TryParse(se[1], out int en))
                    ranges.Add((s, en, id));
            }
        }
        if (ranges.Count == 0) return [];
        ranges.Sort((a, b) => a.start.CompareTo(b.start));

        List<Rune> runes = [];
        foreach (Rune r in text.EnumerateRunes()) runes.Add(r);

        List<ChatMessageSegment> segments = [];
        int cursor = 0;
        foreach ((int start, int end, string id) in ranges)
        {
            // Skip overlapping/invalid/out-of-range ranges; remaining text still renders.
            if (start < cursor || end < start || end >= runes.Count) continue;

            if (start > cursor)
                segments.Add(TextSegment(runes, cursor, start - cursor));
            segments.Add(new ChatMessageSegment
            {
                Type = "emote",
                EmoteId = id,
                Text = RuneString(runes, start, end - start + 1)
            });
            cursor = end + 1;
        }
        if (cursor < runes.Count)
            segments.Add(TextSegment(runes, cursor, runes.Count - cursor));

        return segments;
    }

    private static ChatMessageSegment TextSegment(List<Rune> runes, int start, int length) =>
        new() { Type = "text", Text = RuneString(runes, start, length) };

    private static string RuneString(List<Rune> runes, int start, int length)
    {
        StringBuilder sb = new();
        for (int i = start; i < start + length; i++) sb.Append(runes[i].ToString());
        return sb.ToString();
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
