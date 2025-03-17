#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Events.Subscription;

public class Subscription
{
    [JsonProperty("broadcaster_id")]
    public string BroadcasterId { get; set; }
    [JsonProperty("broadcaster_login")]
    public string BroadcasterLogin { get; set; }
    [JsonProperty("broadcaster_name")]
    public string BroadcasterName { get; set; }
    [JsonProperty("gifter_id")]
    public string GifterId { get; set; }
    [JsonProperty("gifter_login")]
    public string GifterLogin { get; set; }
    [JsonProperty("gifter_name")]
    public string GifterName { get; set; }
    [JsonProperty("is_gift")]
    public bool IsGift { get; set; }
    [JsonProperty("plan_name")]
    public string PlanName { get; set; }
    [JsonProperty("tier")]
    public string Tier { get; set; }
    [JsonProperty("user_id")]
    public string UserId { get; set; }
    [JsonProperty("user_name")]
    public string UserName { get; set; }
    [JsonProperty("user_login")]
    public string UserLogin { get; set; }
}