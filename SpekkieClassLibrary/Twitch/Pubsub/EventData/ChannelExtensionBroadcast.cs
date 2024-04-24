using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChannelExtensionBroadcast : MessageData
{
    public List<string> Messages { get; } = new List<string>();

    public ChannelExtensionBroadcast(string jsonStr)
    {
        foreach (object obj in (IEnumerable<JToken>) JObject.Parse(jsonStr)["content"])
            Messages.Add(obj.ToString());
    }
}