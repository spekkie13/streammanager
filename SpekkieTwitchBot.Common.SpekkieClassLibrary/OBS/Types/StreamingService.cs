using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class StreamingService
{
    [JsonProperty(PropertyName = "streamServiceType")]
    public string? Type { set; get; }

    [JsonProperty(PropertyName = "streamServiceSettings")]
    public StreamingServiceSettings? Settings { set; get; }
}