using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Spotify.Song;

public class Context
{
    [JsonPropertyName("external_urls")]
    public Dictionary<string, string> ExternalUrls { get; set; } = default!;
    [JsonPropertyName("href")]
    public string Href { get; set; } = default!;
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = default!;
}