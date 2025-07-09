using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class RewardRedeemed : ChannelPointsData
{
    [JsonProperty(PropertyName = "timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonProperty(PropertyName = "redemption")]
    public Redemption? Redemption { get; set; }
}