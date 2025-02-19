namespace SpekkieClassLibrary.Spotify.Song;

public class FullTrack
{
    public SimpleAlbum Album { get; set; } = default!;
    public List<SimpleArtist> Artists { get; set; } = default!;
    public List<string> AvailableMarkets { get; set; } = default!;
    public int DiscNumber { get; set; }
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public Dictionary<string, string> ExternalIds { get; set; } = default!;
    public Dictionary<string, string> ExternalUrls { get; set; } = default!;
    public string Href { get; set; } = default!;
    public string Id { get; set; } = default!;
    public bool IsLocal { get; set; }
    public string Name { get; set; } = default!;
    public int Popularity { get; set; }
    public string PreviewUrl { get; set; } = default!;
    public int TrackNumber { get; set; }
    public string Type { get; set; } = default!;
    public string Uri { get; set; } = default!;
}