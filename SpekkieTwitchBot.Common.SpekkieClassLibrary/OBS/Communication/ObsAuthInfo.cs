#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Communication;

public class ObsAuthInfo
{
    [JsonProperty(PropertyName = "challenge")]
    public readonly string Challenge;

    [JsonProperty(PropertyName = "salt")] public readonly string PasswordSalt;

    public ObsAuthInfo(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public ObsAuthInfo()
    {
    }
}