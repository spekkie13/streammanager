using Newtonsoft.Json;

namespace SpekkieTwitchBot.Systems.OBS.Models;

public class ObsConnectionOptions
{
    [JsonProperty(PropertyName = "Obs_Url")]
    public string? ObsUrl { get; set; }

    [JsonProperty(PropertyName = "Password")]
    public string? Password { get; set; }
}