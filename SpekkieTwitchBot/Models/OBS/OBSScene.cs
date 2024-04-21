using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SceneItemDetails = SpekkieTwitchBot.Models.OBS.Types.SceneItemDetails;

namespace SpekkieTwitchBot.Models.OBS;

public class ObsScene
{
    [JsonProperty(PropertyName = "sceneName")]
    public string Name;

    [JsonProperty(PropertyName = "isGroup")]
    public bool IsGroup;

    [JsonProperty(PropertyName = "sources")]
    public List<SceneItemDetails> Items;

    public ObsScene(JObject data)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            NullValueHandling = NullValueHandling.Include
        };
        if (data.ContainsKey("currentProgramSceneName"))
        {
            var newToken = JToken.FromObject(data["currentProgramSceneName"]);
            data.Add("sceneName", newToken);
        }
        JsonConvert.PopulateObject(data.ToString(), this, settings);
    }

    public ObsScene() { }
}