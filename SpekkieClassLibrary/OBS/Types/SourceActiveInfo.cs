using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class SourceActiveInfo
{
    [JsonProperty(PropertyName = "videaActive")]
    public bool VideoActive { get; set; }

    [JsonProperty(PropertyName = "videoShowing")]
    public bool VideoShowing { get; set; }

    public SourceActiveInfo(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public SourceActiveInfo() { }
}