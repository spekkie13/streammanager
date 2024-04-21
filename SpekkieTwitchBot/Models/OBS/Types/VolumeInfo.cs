using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class VolumeInfo
{
    [JsonProperty(PropertyName = "inputVolumeMul")]
    public float VolumeMul { internal set; get; }

    [JsonProperty(PropertyName = "inputVolumeDb")]
    public float VolumeDb { internal set; get; }

    public VolumeInfo(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public VolumeInfo() { }
}