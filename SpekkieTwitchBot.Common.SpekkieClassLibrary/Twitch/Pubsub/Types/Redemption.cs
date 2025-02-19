#nullable disable
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class Redemption
{
    [JsonProperty(PropertyName = "id")] public string Id { get; protected set; }

    [JsonProperty(PropertyName = "user")] public User User { get; protected set; }

    [JsonProperty(PropertyName = "channel_id")]
    public string ChannelId { get; protected set; }

    [JsonProperty(PropertyName = "redeemed_at")]
    public DateTime RedeemedAt { get; protected set; }

    [JsonProperty(PropertyName = "reward")]
    public Reward Reward { get; protected set; }

    [JsonProperty(PropertyName = "user_input")]
    public string UserInput { get; protected set; }

    [JsonProperty(PropertyName = "status")]
    public string Status { get; protected set; }
}