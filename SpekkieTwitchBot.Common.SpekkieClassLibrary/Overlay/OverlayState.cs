namespace SpekkieClassLibrary.Overlay;

public class OverlayState
{
    public string Mode { get; init; } = "war";
    public string Layout { get; init; } = "clashLandscape";
    public string UpdatedAt { get; init; } = "";
    public bool IsWarActive { get; init; }
    public string AccountName { get; init; } = "";
    public OverlayEventInfo Event { get; init; } = new();
    public OverlayWar War { get; init; } = new();
    public OverlaySupport Support { get; init; } = new();
    public OverlayInfo Info { get; init; } = new();
}
