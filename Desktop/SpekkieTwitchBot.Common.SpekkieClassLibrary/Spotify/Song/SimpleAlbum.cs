namespace SpekkieClassLibrary.Spotify.Song;

public class SimpleAlbum
{
    public string AlbumType { get; set; } = null!;
    public List<SimpleArtist> Artists { get; set; } = null!;
    public List<string> AvailableMarkets { get; set; } = null!;
    public Dictionary<string, string> ExternalUrls { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public List<Image> Images { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ReleaseDate { get; set; } = null!;
    public string ReleaseDatePrecision { get; set; } = null!;
    public int TotalTracks { get; set; }
    public string Type { get; set; } = null!;
    public string Uri { get; set; } = null!;
}