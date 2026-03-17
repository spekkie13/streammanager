namespace SpekkieClassLibrary.Spotify.Song;

public class SimpleShow
{
    public List<string> AvailableMarkets { get; set; } = null!;
    public List<Copyright> Copyrights { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string HtmlDescription { get; set; } = null!;
    public bool Explicit { get; set; }
    public Dictionary<string, string> ExternalUrls { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public List<Image> Images { get; set; } = null!;
    public bool IsExternallyHosted { get; set; }
    public List<string> Languages { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Publisher { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Uri { get; set; } = null!;
    public int TotalEpisodes { get; set; } = 0;
}