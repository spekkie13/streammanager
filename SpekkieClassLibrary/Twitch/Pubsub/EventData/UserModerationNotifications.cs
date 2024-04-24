using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Types;
using TwitchLib.PubSub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class UserModerationNotifications : MessageData
{
    public UserModerationNotificationsType Type { get; private set; }

    public UserModerationNotificationsData Data { get; private set; }

    public string RawData { get; private set; }

    public UserModerationNotifications(string jsonStr)
    {
        RawData = jsonStr;
        JToken jtoken = JObject.Parse(jsonStr);
        if (((object) jtoken.SelectToken("type")).ToString() == "automod_caught_message")
        {
            Type = UserModerationNotificationsType.AutomodCaughtMessage;
            Data = (UserModerationNotificationsData) JsonConvert.DeserializeObject<AutomodCaughtResponseMessage>(((object) jtoken.SelectToken("data")).ToString());
        }
        else
            Type = UserModerationNotificationsType.Unknown;
    }
}