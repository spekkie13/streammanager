using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChannelExtensionBroadcast : MessageData
{
    public List<string> Messages { get; } = new();

    public ChannelExtensionBroadcast(string jsonStr)
    {
        foreach (JToken obj in JObject.Parse(jsonStr)["content"]!)
            Messages.Add(obj.ToString());
    }
}