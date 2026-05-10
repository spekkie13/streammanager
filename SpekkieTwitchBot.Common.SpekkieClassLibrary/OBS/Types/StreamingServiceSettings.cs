using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class StreamingServiceSettings
{
    [JsonProperty(PropertyName = "server")]
    public string? Server { set; get; }

    [JsonProperty(PropertyName = "key")] 
    public string? Key { set; get; }

    [JsonProperty(PropertyName = "use_auth")]
    public bool UseAuth { set; get; }

    [JsonProperty(PropertyName = "username")]
    public string? Username { set; get; }

    [JsonProperty(PropertyName = "password")]
    public string? Password { set; get; }
}