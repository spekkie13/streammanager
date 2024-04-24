using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Communication;

public class ServerMessage
{
    [JsonProperty(PropertyName = "op")]
    public MessageTypes OperationCode { set; get; }
    
    [JsonProperty(PropertyName = "d")]
    public JObject? Data { get; set; }
}