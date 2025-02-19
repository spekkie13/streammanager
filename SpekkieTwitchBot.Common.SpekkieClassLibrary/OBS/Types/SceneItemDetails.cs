#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Types;

public class SceneItemDetails
{
    public SceneItemDetails(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public SceneItemDetails()
    {
    }

    [JsonProperty(PropertyName = "sceneItemId")]
    public int ItemId { set; get; }

    [JsonProperty(PropertyName = "inputKind")]
    public string SourceKind { set; get; }

    [JsonProperty(PropertyName = "sourceName")]
    public string SourceName { set; get; }

    [JsonProperty(PropertyName = "sourceType")]
    public SceneItemSourceType SourceType { set; get; }
}