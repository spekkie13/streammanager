using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Abstract;

public abstract class Input
{
    [JsonProperty(PropertyName = "inputName")]
    public string? InputName { get; set; }

    [JsonProperty(PropertyName = "inputKind")]
    public string? InputKind { get; set; }

    protected Input(JObject body) => JsonConvert.PopulateObject(body.ToString(), this);

    protected Input()
    {
    }
}