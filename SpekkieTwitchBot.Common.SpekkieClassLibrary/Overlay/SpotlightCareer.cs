namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// The spotlighted player's career stats from the Competitive Clash Network (their attack/defense
/// history across wars), keyed by player tag. Either side may be null if CCN has no data.
/// </summary>
public class SpotlightCareer
{
    public SpotlightStatLine? Offense { get; init; }
    public SpotlightStatLine? Defense { get; init; }
}
