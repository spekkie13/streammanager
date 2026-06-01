using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Shared serialization + atomic-write helpers for the file-based overlay state files
/// (overlay-state.json, chat-state.json). The overlay polls these files, so a write must
/// never expose a partial file.
/// </summary>
internal static class OverlayJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Serialize to a sibling temp file, flush to disk, then atomically replace.
    public static async Task WriteAtomicAsync(string path, string json, CancellationToken ct)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string tmp = path + ".tmp";
        await using (FileStream fs = new(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (StreamWriter sw = new(fs))
        {
            await sw.WriteAsync(json.AsMemory(), ct);
            await sw.FlushAsync(ct);
            fs.Flush(flushToDisk: true);
        }

        // File.Move with overwrite maps to MoveFileEx(MOVEFILE_REPLACE_EXISTING) on Windows,
        // which is atomic for same-volume moves; the overlay always sees a complete file.
        File.Move(tmp, path, overwrite: true);
    }
}
