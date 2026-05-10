using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Commands;

public class TimedMessages
{
    [JsonProperty("TimedMessages")]
    public List<TimedMessage> Messages { get; set; } = [];
}