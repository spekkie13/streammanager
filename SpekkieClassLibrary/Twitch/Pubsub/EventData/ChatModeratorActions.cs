using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChatModeratorActions : MessageData
{
    public string Type { get; }
    public string ModerationAction { get; }
    public List<string> Args { get; } = new List<string>();
    public string CreatedBy { get; }
    public string CreatedByUserId { get; }
    public string TargetUserId { get; }

    public ChatModeratorActions(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr).SelectToken("data");
        Type = ((object)jtoken.SelectToken("type"))?.ToString();
        ModerationAction = ((object)jtoken.SelectToken("moderation_action"))?.ToString();
        if (jtoken.SelectToken("args") != null)
        {
            foreach (object obj in (IEnumerable<JToken>)jtoken.SelectToken("args"))
                Args.Add(obj.ToString());
        }

        CreatedBy = ((object)jtoken.SelectToken("created_by")).ToString();
        CreatedByUserId = ((object)jtoken.SelectToken("created_by_user_id")).ToString();
        TargetUserId = ((object)jtoken.SelectToken("target_user_id")).ToString();
    }
}