#nullable disable
namespace SpekkieClassLibrary.Spotify.Song;

public abstract class Album
{
    public string AlbumType { get; set; }
    public List<Artist> Artists { get; set; }
    public List<string> AvailableMarkets { get; set; }
    public ExternalUrls ExternalUrls { get; set; }
    public string Href { get; set; }
    public string Id { get; set; }
    public List<object> Images { get; set; }
    public string Name { get; set; }
    public string ReleaseDatePrecision { get; set; }
    public string ReleaseDate { get; set; }
    public int TotalTracks { get; set; }
    public string Type { get; set; }
    public string Uri { get; set; }
}