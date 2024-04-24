using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SpekkieClassLibrary.Spotify.Enum;

namespace SpekkieClassLibrary.Spotify;

public interface IPlayableItem
{
    [JsonConverter(typeof(StringEnumConverter))]
    ItemType Type { get; }
}