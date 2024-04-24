using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify.Converter;
using SpekkieClassLibrary.Spotify.Enum;

namespace SpekkieClassLibrary.Spotify.Song
{
    public class CurrentlyPlaying(ItemType type) : IPlayableItem
    {
        public Context Context { get; set; } = default!;
        public string CurrentlyPlayingType { get; set; } = default!;
        public bool IsPlaying { get; set; }

        [JsonConverter(typeof(PlayableItemConverter))]
        public IPlayableItem Item { get; set; } = default!;
        public int? ProgressMs { get; set; }
        public long Timestamp { get; set; }
        public ItemType Type { get; } = type;
    }
}