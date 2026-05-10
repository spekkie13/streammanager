using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class OutputStatus
{
    [JsonProperty(PropertyName = "outputActive")]
    public readonly bool IsActive;

    public OutputStatus(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public OutputStatus()
    {
    }

    [JsonProperty(PropertyName = "outputReconnecting")]
    public bool IsReconnecting { get; set; }

    [JsonProperty(PropertyName = "outputTimecode")]
    public string? TimeCode { get; set; }

    [JsonProperty(PropertyName = "outputDuration")]
    public long Duration { get; set; }

    [JsonProperty(PropertyName = "outputCongestion")]
    public double Congestion { get; set; }

    [JsonProperty(PropertyName = "outputBytes")]
    public long BytesSent { get; set; }

    [JsonProperty(PropertyName = "outputSkippedFrames")]
    public long SkippedFrames { get; set; }

    [JsonProperty(PropertyName = "outputTotalFrames")]
    public long TotalFrames { get; set; }
}