using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class SceneBasicInfo
{
    [JsonProperty(PropertyName = "sceneName")]
    public string Name { set; get; }

    [JsonProperty(PropertyName = "sceneIndex")]
    public string Index { set; get; }
}