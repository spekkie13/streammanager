using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class FilterSettings
{
    [JsonProperty(PropertyName = "filterName")]
    public string Name { set; get; }

    [JsonProperty(PropertyName = "filterKind")]
    public string Kind { set; get; }

    [JsonProperty(PropertyName = "filterIndex")]
    public int Index { get; set; }

    [JsonProperty(PropertyName = "filterEnabled")]
    public bool IsEnabled { set; get; }

    [JsonProperty(PropertyName = "filterSettings")]
    public JObject Settings { set; get; }
}