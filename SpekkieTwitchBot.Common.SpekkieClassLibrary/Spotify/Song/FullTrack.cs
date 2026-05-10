namespace SpekkieClassLibrary.Spotify.Song;

public class FullTrack
{
    public SimpleAlbum? Album { get; set; }
    public List<SimpleArtist> Artists { get; set; } = null!;
    public List<string> AvailableMarkets { get; set; } = null!;
    public int DiscNumber { get; set; }
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public Dictionary<string, string> ExternalIds { get; set; } = null!;
    public Dictionary<string, string> ExternalUrls { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public bool IsLocal { get; set; }
    public string Name { get; set; } = null!;
    public int Popularity { get; set; }
    public string PreviewUrl { get; set; } = null!;
    public int TrackNumber { get; set; }
    public string Type { get; set; } = null!;
    public string Uri { get; set; } = null!;
}