using Newtonsoft.Json;
using SpekkieTwitchBot.Models.Spotify.Converter;
using SpekkieTwitchBot.Models.Spotify.Enum;

namespace SpekkieTwitchBot.Models.Spotify.Song
{
    public class CurrentlyPlaying : IPlayableItem
    {
        public Context Context { get; set; } = default!;
        public string CurrentlyPlayingType { get; set; } = default!;
        public bool IsPlaying { get; set; }

        [JsonConverter(typeof(PlayableItemConverter))]
        public IPlayableItem Item { get; set; } = default!;
        public int? ProgressMs { get; set; }
        public long Timestamp { get; set; }
        public ItemType Type { get; }
    }
}