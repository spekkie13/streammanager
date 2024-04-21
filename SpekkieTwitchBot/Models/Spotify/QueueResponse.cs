using Newtonsoft.Json;
using SpekkieTwitchBot.Models.Spotify.Converter;

namespace SpekkieTwitchBot.Models.Spotify
{
    public class QueueResponse
    {
        [JsonConverter(typeof(PlayableItemConverter))]
        public IPlayableItem CurrentlyPlaying { get; set; } = default!;
        [JsonProperty(ItemConverterType = typeof(PlayableItemConverter))]
        public List<IPlayableItem> Queue { get; set; } = default!;
    }
}