#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Events.ChannelPoint;

public class Redemption
{
    [JsonProperty(PropertyName = "broadcaster_name")]
    public string BroadcasterName { get; set; }

    [JsonProperty(PropertyName = "broadcaster_login")]
    public string BroadcasterLogin { get; set; }

    [JsonProperty(PropertyName = "broadcaster_id")]
    public string BroadcasterId { get; set; }

    [JsonProperty(PropertyName = "id")] public string Id { get; set; }

    [JsonProperty(PropertyName = "user_id")]
    public string UserId { get; set; }

    [JsonProperty(PropertyName = "user_name")]
    public string UserName { get; set; }

    [JsonProperty(PropertyName = "user_input")]
    public string UserInput { get; set; }

    [JsonProperty(PropertyName = "status")]
    public string Status { get; set; }

    [JsonProperty(PropertyName = "redeemed_at")]
    public string RedeemedAt { get; set; }

    [JsonProperty(PropertyName = "reward")]
    public Reward Reward { get; set; }
}