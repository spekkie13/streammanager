using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class ObsVersion
{
    [JsonProperty(PropertyName = "obsWebSocketVersion")]
    public string PluginVersion { get; internal set; }

    [JsonProperty(PropertyName = "obsVersion")]
    public string OBSStudioVersion { get; internal set; }

    [JsonProperty(PropertyName = "rpcVersion")]
    public double Version { internal set; get; }

    [JsonProperty(PropertyName = "availableRequests")]
    public List<string> AvailableRequests { get; internal set; }

    [JsonProperty(PropertyName = "supportedImageFormats")]
    public List<string> SupportedImageFormats { get; internal set; }

    [JsonProperty(PropertyName = "platform")]
    public string Platform { get; internal set; }

    [JsonProperty(PropertyName = "platformDescription")]
    public string PlatformDescription { get; internal set; }

    public ObsVersion(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public ObsVersion() { }
}