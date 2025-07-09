using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class AutomodQueue : MessageData
{
    public AutomodQueue(string jsonStr)
    {
        RawData = jsonStr;
        JToken jtoken = JObject.Parse(jsonStr);
        if (jtoken.SelectToken("type")?.ToString() == "automod_caught_message")
        {
            Type = AutomodQueueType.CaughtMessage;
            Data = JsonConvert.DeserializeObject<AutomodCaughtMessage>(jtoken.SelectToken("data")?.ToString() ?? "");
        }
        else
        {
            Type = AutomodQueueType.Unknown;
        }
    }

    public AutomodQueueType Type { get; private set; }
    public AutomodQueueData? Data { get; private set; }
    public string? RawData { get; private set; }
}