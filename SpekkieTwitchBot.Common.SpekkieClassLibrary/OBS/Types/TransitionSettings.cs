#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class TransitionSettings
{
    public TransitionSettings(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public TransitionSettings()
    {
    }

    [JsonProperty(PropertyName = "transitionName")]
    public string Name { internal set; get; }

    [JsonProperty(PropertyName = "transitionDuration")]
    public int? Duration { internal set; get; }

    [JsonProperty(PropertyName = "transitionKind")]
    public string Kind { internal set; get; }

    [JsonProperty(PropertyName = "transitionFixed")]
    public bool IsFixed { internal set; get; }

    [JsonProperty(PropertyName = "transitionConfigurable")]
    public bool IsConfigurable { internal set; get; }

    [JsonProperty(PropertyName = "transitionSettings")]
    public JObject Settings { get; set; }
}