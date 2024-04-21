using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet.Types;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class InputBasicInfo : Input
{
    [JsonProperty(PropertyName = "unversionedInputKind")]
    public string UnversionedKind { get; set; }

    public InputBasicInfo(JObject body) : base(body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public InputBasicInfo() { }
}