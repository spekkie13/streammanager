using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Events.Follower;

public class Follower
{
    [JsonProperty("user_id")]
    public string UserId { get; set; } = null!;

    [JsonProperty("user_login")]
    public string UserLogin { get; set; } = null!;

    [JsonProperty("user_name")]
    public string UserName { get; set; } = null!;

    [JsonProperty("followed_at")]
    public DateTime FollowedAt { get; set; }
}