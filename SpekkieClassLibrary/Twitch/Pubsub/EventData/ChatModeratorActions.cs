using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChatModeratorActions : MessageData
{
    public string Type { get; }
    public string ModerationAction { get; }
    public List<string> Args { get; } = new();
    public string CreatedBy { get; }
    public string CreatedByUserId { get; }
    public string TargetUserId { get; }

    public ChatModeratorActions(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr).SelectToken("data");
        Type = jtoken?.SelectToken("type")?.ToString();
        ModerationAction = jtoken?.SelectToken("moderation_action")?.ToString();
        if (jtoken?.SelectToken("args") != null)
        {
            foreach (JToken obj in jtoken.SelectToken("args")!)
                Args.Add(obj.ToString());
        }

        CreatedBy = jtoken?.SelectToken("created_by")?.ToString();
        CreatedByUserId = jtoken?.SelectToken("created_by_user_id")?.ToString();
        TargetUserId = jtoken?.SelectToken("target_user_id")?.ToString();
    }
}