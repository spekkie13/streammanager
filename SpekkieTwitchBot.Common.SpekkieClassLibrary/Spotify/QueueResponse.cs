using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify.Converter;
using SpekkieClassLibrary.Spotify.Song;

namespace SpekkieClassLibrary.Spotify;

public class QueueResponse
{
    public FullTrack CurrentlyPlaying { get; set; } = null!;

    [JsonProperty(ItemConverterType = typeof(PlayableItemConverter))]
    public List<FullTrack> Queue { get; set; } = null!;
}