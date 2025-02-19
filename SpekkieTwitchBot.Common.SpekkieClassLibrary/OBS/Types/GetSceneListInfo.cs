#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class GetSceneListInfo
{
    [JsonProperty(PropertyName = "currentProgramSceneName")]
    public string CurrentProgramSceneName { set; get; }

    [JsonProperty(PropertyName = "currentPreviewSceneName")]
    public string CurrentPreviewSceneName { set; get; }

    [JsonProperty(PropertyName = "scenes")]
    public List<SceneBasicInfo> Scenes { set; get; }
}