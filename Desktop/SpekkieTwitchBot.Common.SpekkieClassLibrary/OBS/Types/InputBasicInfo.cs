using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Abstract;

namespace SpekkieClassLibrary.OBS.Types;

public class InputBasicInfo : Input
{
    public InputBasicInfo(JObject body) : base(body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public InputBasicInfo()
    {
    }

    [JsonProperty(PropertyName = "unversionedInputKind")]
    public string? UnversionedKind { get; set; }
}