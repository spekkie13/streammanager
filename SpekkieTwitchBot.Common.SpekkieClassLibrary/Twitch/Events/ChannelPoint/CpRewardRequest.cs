#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Events.ChannelPoint;

public class CpRewardRequest
{
    [JsonProperty(PropertyName = "data")] public Redemption[] Data { get; set; }
}