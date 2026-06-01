namespace SpekkieClassLibrary.Overlay;

public class OverlayEventConfig
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public List<string> Socials { get; set; } = [];
    public OverlayInfo Info { get; set; } = new();
}
