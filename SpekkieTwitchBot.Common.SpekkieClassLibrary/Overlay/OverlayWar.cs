namespace SpekkieClassLibrary.Overlay;

public class OverlayWar
{
    public OverlayTeam Home { get; init; } = new();
    public OverlayTeam Away { get; init; } = new();
    public OverlayWarStats Stats { get; init; } = new();
}
