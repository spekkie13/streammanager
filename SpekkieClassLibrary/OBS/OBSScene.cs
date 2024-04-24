using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SceneItemDetails = SpekkieClassLibrary.OBS.Types.SceneItemDetails;

namespace SpekkieClassLibrary.OBS;

public class ObsScene
{
    [JsonProperty(PropertyName = "sceneName")]
    public string? Name;

    [JsonProperty(PropertyName = "isGroup")]
    public bool IsGroup;

    [JsonProperty(PropertyName = "sources")]
    public List<SceneItemDetails>? Items;

    public ObsScene(JObject data)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            NullValueHandling = NullValueHandling.Include
        };
        JObject? currentProgramSceneName = (JObject?) data["currentProgramSceneName"];
        if (data.ContainsKey("currentProgramSceneName") && currentProgramSceneName != null)
        {
            var newToken = JToken.FromObject(currentProgramSceneName);
            data.Add("sceneName", newToken);
        }
        JsonConvert.PopulateObject(data.ToString(), this, settings);
    }

    public ObsScene() { }
}