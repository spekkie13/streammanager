using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable disable
namespace SpekkieClassLibrary.OBS.Types;

public class RecordingStatus
{
    [JsonProperty(PropertyName = "outputActive")]
    public bool IsRecording { set; get; }

    [JsonProperty(PropertyName = "outputPaused")]
    public bool IsRecordingPaused { set; get; }

    [JsonProperty(PropertyName = "outputTimecode")]
    public string RecordTimecode { set; get; }

    [JsonProperty(PropertyName = "outputDuration")]
    public long RecordingDuration { set; get; }

    [JsonProperty(PropertyName = "outputBytes")]
    public long RecordingBytes { set; get; }

    public bool IsActive { get; set; }

    public RecordingStatus(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }
    
    public RecordingStatus() { }
}