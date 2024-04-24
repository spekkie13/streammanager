using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify.Converter;

namespace SpekkieClassLibrary.Spotify
{
    public class QueueResponse
    {
        [JsonConverter(typeof(PlayableItemConverter))]
        public IPlayableItem CurrentlyPlaying { get; set; } = default!;
        [JsonProperty(ItemConverterType = typeof(PlayableItemConverter))]
        public List<IPlayableItem> Queue { get; set; } = default!;
    }
}