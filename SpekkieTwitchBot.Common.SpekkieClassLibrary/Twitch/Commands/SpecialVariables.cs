using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Commands;

public class SpecialVariables
{
    [JsonProperty("SpecialVariables")]
    public List<SpecialVariable> Variables { get; set; } = new ();
}