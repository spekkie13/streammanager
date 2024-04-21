using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Communication;

public class OBSAuthInfo
{
    [JsonProperty(PropertyName = "challenge")]
    public readonly string Challenge;

    [JsonProperty(PropertyName = "salt")]
    public readonly string PasswordSalt;

    public OBSAuthInfo(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }
        
    public OBSAuthInfo() { }
}