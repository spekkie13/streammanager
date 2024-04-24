using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class InputVolume
{
    [JsonProperty(PropertyName = "inputName")]
    public string? InputName { set; get; }

    [JsonProperty(PropertyName = "inputVolumeMul")]
    public float InputVolumeMul { get; set; }
    
    [JsonProperty(PropertyName = "inputVolumeDb")]
    public float InputVolumeDb { get; set; }

    public InputVolume(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }
    
    public InputVolume() { }
}