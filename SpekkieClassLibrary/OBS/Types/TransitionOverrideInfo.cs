using Newtonsoft.Json;

#nullable disable
namespace SpekkieClassLibrary.OBS.Types;

public class TransitionOverrideInfo
{
    [JsonProperty(PropertyName = "transitionName")]
    public string Name { internal set; get; }

    [JsonProperty(PropertyName = "transitionDuration")]
    public int Duration { internal set; get; }
}