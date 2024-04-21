using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class MediaInputStatus
{
    [JsonProperty(PropertyName = "mediaState")]
    public string State { get; set; }

    [JsonProperty(PropertyName = "mediaDuration")]
    public int? Duration { get; set; }

    [JsonProperty(PropertyName = "mediaCursor")]
    public int Cursor { get; set; }

    public MediaInputStatus(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public MediaInputStatus() { }
}