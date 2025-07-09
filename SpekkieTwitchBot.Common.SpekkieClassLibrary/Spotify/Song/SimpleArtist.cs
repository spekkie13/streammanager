namespace SpekkieClassLibrary.Spotify.Song;

public abstract class SimpleArtist
{
    public Dictionary<string, string> ExternalUrls { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Uri { get; set; } = null!;
}