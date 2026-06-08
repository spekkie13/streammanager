using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Resolves the overlay mode written into overlay-state.json. Mode is automatic by default but can
/// be manually overridden via Settings/overlay-mode-override.txt:
///   - "war" / "farming" / "event" / "startingSoon" / "brb" -> that mode is forced.
///   - "auto", empty, missing, or anything unrecognized -> automatic mode.
/// Matching is case-insensitive and trimmed, but the canonical casing (e.g. "startingSoon") is
/// returned so the browser overlay's mode comparison matches.
/// Automatic mode v1: "war" when a war is active, otherwise "farming".
/// The file is re-read on every call so edits take effect on the writer's next tick.
/// </summary>
public sealed class OverlayModeController
{
    private static readonly string DefaultOverridePath =
        Path.Combine(BotPaths.BaseDir, "Settings", "overlay-mode-override.txt");

    private static readonly string[] ManualModes =
        { OverlayModes.War, OverlayModes.Farming, OverlayModes.Event, OverlayModes.StartingSoon, OverlayModes.Brb };

    private readonly IWarService _warService;
    private readonly Logger _logger;
    private readonly string _overridePath;

    // Last override value we warned about, so a standing typo is logged once rather than every tick.
    private string? _lastWarnedOverride;

    public OverlayModeController(IWarService warService, Logger logger, string? overridePath = null)
    {
        _warService = warService;
        _logger = logger;
        _overridePath = overridePath ?? DefaultOverridePath;
    }

    public string Resolve()
    {
        string ovr = ReadOverride();

        // Return the canonical constant (not the raw input) so casing like "startingSoon" is preserved
        // for the browser overlay, while still matching the file value case-insensitively.
        string? manual = Array.Find(ManualModes, m => string.Equals(m, ovr, StringComparison.OrdinalIgnoreCase));
        if (manual != null) return manual;

        WarnOnUnrecognized(ovr);

        // "auto", empty, missing, or unrecognized -> automatic mode.
        return _warService.IsWarActive ? OverlayModes.War : OverlayModes.Farming;
    }

    // Warn when the file holds non-empty content that is neither a manual mode nor explicit "auto"
    // (i.e. a typo like "farmin"). Deduped so the same bad value is only logged once.
    private void WarnOnUnrecognized(string ovr)
    {
        bool isTypo = ovr.Length > 0 && !string.Equals(ovr, OverlayModes.Auto, StringComparison.OrdinalIgnoreCase);
        if (isTypo && !string.Equals(ovr, _lastWarnedOverride, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"[Overlay] Unrecognized mode override '{ovr}', falling back to auto.");
            _lastWarnedOverride = ovr;
        }
        else if (!isTypo)
        {
            _lastWarnedOverride = null;
        }
    }

    // Read tolerantly: a missing file or any read error (e.g. a concurrent editor holding a lock)
    // returns "" so Resolve falls back to automatic mode.
    private string ReadOverride()
    {
        try
        {
            if (!File.Exists(_overridePath)) return "";

            using FileStream fs = new(_overridePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fs);
            return reader.ReadToEnd().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Overlay] Failed to read mode override: {ex.Message}");
            return "";
        }
    }
}