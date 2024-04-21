using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SpekkieTwitchBot.Models.Spotify.Enum;

public interface IPlayableItem
{
    [JsonConverter(typeof(StringEnumConverter))]
    ItemType Type { get; }
}