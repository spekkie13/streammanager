using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Communication;

public class ObsAuthInfo
{
    [JsonProperty(PropertyName = "challenge")]
    public readonly string Challenge = null!;

    [JsonProperty(PropertyName = "salt")] 
    public readonly string PasswordSalt = null!;

    public ObsAuthInfo(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public ObsAuthInfo()
    {
    }
}