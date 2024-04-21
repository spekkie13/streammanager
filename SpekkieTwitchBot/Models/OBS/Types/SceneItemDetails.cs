using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet.Types;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class SceneItemDetails
{
    [JsonProperty(PropertyName = "sceneItemId")]
    public int ItemId { set; get; }

    [JsonProperty(PropertyName = "inputKind")]
    public string SourceKind { set; get; }

    [JsonProperty(PropertyName = "sourceName")]
    public string SourceName { set; get; }

    [JsonProperty(PropertyName = "sourceType")]
    public SceneItemSourceType SourceType { set; get; }

    public SceneItemDetails(JObject data)
    {
        if (data != null)
        {
            JsonConvert.PopulateObject(data.ToString(), this);
        }
    }

    public SceneItemDetails() { }
}