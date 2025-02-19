using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class VirtualCamStatus
{
    public VirtualCamStatus(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public VirtualCamStatus()
    {
    }

    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }
}