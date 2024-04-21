using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class VirtualCamStatus
{
    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }
    
    public VirtualCamStatus(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }
    
    public VirtualCamStatus()
    {
    }
}