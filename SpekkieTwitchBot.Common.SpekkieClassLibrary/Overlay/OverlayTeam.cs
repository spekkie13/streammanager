namespace SpekkieClassLibrary.Overlay;

public class OverlayTeam
{
    public string Name { get; init; } = "";
    public string LogoFile { get; init; } = "";
    public int Stars { get; init; }
    public string DestructionPct { get; init; } = "";
    public List<OverlayPlayer> Players { get; init; } = [];
}
