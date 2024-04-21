using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class GetTransitionListInfo
{
    [JsonProperty(PropertyName = "currentSceneTransitionName")]
    public string CurrentTransition { set; get; }

    [JsonProperty(PropertyName = "currentSceneTransitionKind")]
    public string CurrentTransitionKing { set; get; }

    [JsonProperty(PropertyName = "transitions")]
    public List<OBSWebsocketDotNet.Types.TransitionSettings> Transitions { set; get; }
}