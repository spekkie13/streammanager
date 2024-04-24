using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class SceneBasicInfo
{
    [JsonProperty(PropertyName = "sceneName")]
    public string? Name { set; get; }

    [JsonProperty(PropertyName = "sceneIndex")]
    public string? Index { set; get; }
}