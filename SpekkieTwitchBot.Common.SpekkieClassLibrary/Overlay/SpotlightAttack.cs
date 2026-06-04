namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// A single attack result, pre-formatted for direct display (no math in the frontend).
/// </summary>
public class SpotlightAttack
{
    public int Stars { get; init; }
    public string Pct { get; init; } = "";
    public string Duration { get; init; } = "";
}
