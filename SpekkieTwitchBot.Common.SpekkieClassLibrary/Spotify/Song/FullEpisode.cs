using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SpekkieClassLibrary.Spotify.Enum;

namespace SpekkieClassLibrary.Spotify.Song;

public class FullEpisode : IPlayableItem
{
    public string AudioPreviewUrl { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string HtmlDescription { get; set; } = null!;
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public Dictionary<string, string> ExternalUrls { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public List<Image> Images { get; set; } = null!;
    public bool IsExternallyHosted { get; set; }
    public bool IsPlayable { get; set; }
    public List<string> Languages { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ReleaseDate { get; set; } = null!;
    public string ReleaseDatePrecision { get; set; } = null!;
    public ResumePoint ResumePoint { get; set; } = null!;
    public SimpleShow Show { get; set; } = null!;
    public string Uri { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemType Type { get; set; }
}