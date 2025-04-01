using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Commands;

public class TextCommands
{
    [JsonProperty("TextCommands")]
    public List<TextCommand> Commands { get; set; } = new List<TextCommand>();
}