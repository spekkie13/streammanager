namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// A player's career offense or defense line from CCN, pre-formatted for display. Counts are raw;
/// rates/averages are display strings (e.g. "62.5%", "2:18").
/// </summary>
public class SpotlightStatLine
{
    public int Attacks { get; init; }
    public int Triples { get; init; }
    public int Doubles { get; init; }
    public int Singles { get; init; }
    public int Zeros { get; init; }
    public string TripleRate { get; init; } = "";
    public double AvgStars { get; init; }
    public string AvgPct { get; init; } = "";
    public string AvgDuration { get; init; } = "";
}
