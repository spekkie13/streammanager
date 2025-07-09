using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class GetTransitionListInfo
{
    [JsonProperty(PropertyName = "currentSceneTransitionName")]
    public string? CurrentTransition { set; get; }

    [JsonProperty(PropertyName = "currentSceneTransitionKind")]
    public string? CurrentTransitionKing { set; get; }

    [JsonProperty(PropertyName = "transitions")]
    public List<TransitionSettings>? Transitions { set; get; }
}