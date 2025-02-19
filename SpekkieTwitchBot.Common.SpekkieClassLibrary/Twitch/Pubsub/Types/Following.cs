#nullable disable
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class Following : MessageData
{
    public Following(string jsonStr)
    {
        var jobject = JObject.Parse(jsonStr);
        DisplayName = jobject["display_name"]?.ToString();
        Username = jobject["username"]?.ToString();
        UserId = jobject["user_id"]?.ToString();
    }

    public string DisplayName { get; }

    public string Username { get; }

    public string UserId { get; }

    public string FollowedChannelId { get; set; }
}