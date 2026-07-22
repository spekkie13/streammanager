using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Resolves the overlay layout written into overlay-state.json. Layout defaults to
/// "clashLandscape" but can be manually overridden via Settings/layout-mode-override.txt:
///   - "clashLandscape" / "fullHdGame" / "ipadPortrait" -> that layout is forced.
///   - "auto", empty, missing, or anything unrecognized -> the default "clashLandscape".
/// Matching is case-insensitive and trimmed, but the canonical casing (e.g. "fullHdGame") is
/// returned so the browser overlay's layout comparison matches.
/// Unlike mode, layout has no automatic source, so the fallback is simply the default layout.
/// The file is re-read on every call so edits take effect on the writer's next tick.
/// </summary>
public sealed class OverlayLayoutController
{
    private static readonly string DefaultOverridePath =
        Path.Combine(BotPaths.BaseDir, "Settings", "layout-mode-override.txt");

    private static readonly string[] ManualLayouts =
        { OverlayLayouts.ClashLandscape, OverlayLayouts.FullHdGame, OverlayLayouts.IpadPortrait };

    private readonly Logger _logger;
    private readonly string _overridePath;

    // Last override value we warned about, so a standing typo is logged once rather than every tick.
    private string? _lastWarnedOverride;

    public OverlayLayoutController(Logger logger, string? overridePath = null)
    {
        _logger = logger;
        _overridePath = overridePath ?? DefaultOverridePath;
    }

    public string Resolve()
    {
        string ovr = ReadOverride();

        // Return the canonical constant (not the raw input) so casing like "fullHdGame" is preserved
        // for the browser overlay, while still matching the file value case-insensitively.
        string? manual = Array.Find(ManualLayouts, l => string.Equals(l, ovr, StringComparison.OrdinalIgnoreCase));
        if (manual != null) return manual;

        WarnOnUnrecognized(ovr);

        // "auto", empty, missing, or unrecognized -> the default layout.
        return OverlayLayouts.ClashLandscape;
    }

    // Warn when the file holds non-empty content that is neither a manual layout nor explicit "auto"
    // (i.e. a typo like "fullHd"). Deduped so the same bad value is only logged once.
    private void WarnOnUnrecognized(string ovr)
    {
        bool isTypo = ovr.Length > 0 && !string.Equals(ovr, OverlayLayouts.Auto, StringComparison.OrdinalIgnoreCase);
        if (isTypo && !string.Equals(ovr, _lastWarnedOverride, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"[Overlay] Unrecognized layout override '{ovr}', falling back to clashLandscape.");
            _lastWarnedOverride = ovr;
        }
        else if (!isTypo)
        {
            _lastWarnedOverride = null;
        }
    }

    // Read tolerantly: a missing file or any read error (e.g. a concurrent editor holding a lock)
    // returns "" so Resolve falls back to the default layout.
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
            _logger.LogError($"[Overlay] Failed to read layout override: {ex.Message}");
            return "";
        }
    }
}
