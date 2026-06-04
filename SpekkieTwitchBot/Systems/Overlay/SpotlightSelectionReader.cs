using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>The player the operator has selected to spotlight: a team plus a 1-based map position.</summary>
public sealed record SpotlightSelection(string Team, int Position);

/// <summary>
/// Reads Settings/spotlight-selection.txt, the single thing the StreamDeck controls. Supported values:
///   "home:1".."home:5", "away:1".."away:5"  -> that player is spotlighted.
///   "off", empty, missing, locked, or anything unrecognized -> null (no spotlight).
/// Read tolerantly with FileShare.ReadWrite so a concurrent writer never makes this throw, mirroring
/// <see cref="OverlayModeController"/>. The file is re-read on every call so a button press takes effect
/// on the writer's next tick.
/// </summary>
public sealed class SpotlightSelectionReader
{
    private const int MinPosition = 1;
    private const int MaxPosition = 5; // 5v5 only.

    private static readonly string DefaultSelectionPath =
        Path.Combine(BotPaths.BaseDir, "Settings", "spotlight-selection.txt");

    private readonly Logger _logger;
    private readonly string _selectionPath;

    // Last raw value we warned about, so a standing typo is logged once rather than every tick.
    private string? _lastWarnedSelection;

    public SpotlightSelectionReader(Logger logger, string? selectionPath = null)
    {
        _logger = logger;
        _selectionPath = selectionPath ?? DefaultSelectionPath;
    }

    /// <summary>Returns the current selection, or null when nothing valid is selected ("off").</summary>
    public SpotlightSelection? Read()
    {
        string raw = ReadFile();
        if (raw.Length == 0 || string.Equals(raw, "off", StringComparison.OrdinalIgnoreCase))
        {
            _lastWarnedSelection = null;
            return null;
        }

        SpotlightSelection? selection = Parse(raw);
        if (selection == null) WarnOnUnrecognized(raw);
        else _lastWarnedSelection = null;

        return selection;
    }

    private static SpotlightSelection? Parse(string raw)
    {
        string[] parts = raw.Split(':', 2);
        if (parts.Length != 2) return null;

        string team = parts[0].Trim().ToLowerInvariant();
        if (team is not ("home" or "away")) return null;

        if (!int.TryParse(parts[1].Trim(), out int position)) return null;
        if (position is < MinPosition or > MaxPosition) return null;

        return new SpotlightSelection(team, position);
    }

    private void WarnOnUnrecognized(string raw)
    {
        if (string.Equals(raw, _lastWarnedSelection, StringComparison.OrdinalIgnoreCase)) return;
        _logger.LogWarning($"[Overlay] Unrecognized spotlight selection '{raw}', treating as off.");
        _lastWarnedSelection = raw;
    }

    // A missing file or any read error (e.g. a concurrent editor holding a lock) returns "" so the
    // caller treats it as "off".
    private string ReadFile()
    {
        try
        {
            if (!File.Exists(_selectionPath)) return "";

            using FileStream fs = new(_selectionPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fs);
            return reader.ReadToEnd().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Overlay] Failed to read spotlight selection: {ex.Message}");
            return "";
        }
    }
}
