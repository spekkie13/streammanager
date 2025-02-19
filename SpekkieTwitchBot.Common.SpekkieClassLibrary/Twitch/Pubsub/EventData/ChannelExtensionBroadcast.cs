#nullable disable
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChannelExtensionBroadcast : MessageData
{
    public ChannelExtensionBroadcast(string jsonStr)
    {
        foreach (var obj in JObject.Parse(jsonStr)["content"]!)
            Messages.Add(obj.ToString());
    }

    public List<string> Messages { get; } = new();
}