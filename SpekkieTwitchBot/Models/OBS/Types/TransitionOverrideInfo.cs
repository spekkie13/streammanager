using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class TransitionOverrideInfo
{
    [JsonProperty(PropertyName = "transitionName")]
    public string Name { internal set; get; }

    [JsonProperty(PropertyName = "transitionDuration")]
    public int Duration { internal set; get; }
}