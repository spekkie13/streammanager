#nullable disable
using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch.Events.Follower;

public class FollowerRequest
{
    public int Total { get; set; }
    [JsonPropertyName("data")]
    public Follower[] Data { get; set; }
}