#nullable disable
using Newtonsoft.Json.Linq;
using TwitchLib.PubSub.Models.Responses.Messages;

namespace SpekkieTwitchBot.Models.Twitch.Pubsub;

public class Following : MessageData
{
    public string DisplayName { get; }

    public string Username { get; }

    public string UserId { get; }

    public string FollowedChannelId { get; internal set; }

    public Following(string jsonStr)
    {
        JObject jobject = JObject.Parse(jsonStr);
        DisplayName = jobject["display_name"].ToString();
        Username = jobject["username"].ToString();
        UserId = jobject["user_id"].ToString();
    }
}