using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class SourceTracks
{
    [JsonProperty(PropertyName = "1")]
    public bool IsTrack1Active { set; get; }

    [JsonProperty(PropertyName = "2")]
    public bool IsTrack2Active { set; get; }

    [JsonProperty(PropertyName = "3")]
    public bool IsTrack3Active { set; get; }

    [JsonProperty(PropertyName = "4")]
    public bool IsTrack4Active { set; get; }

    [JsonProperty(PropertyName = "5")]
    public bool IsTrack5Active { set; get; }

    [JsonProperty(PropertyName = "6")]
    public bool IsTrack6Active { set; get; }

    public SourceTracks(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public SourceTracks() { }
}