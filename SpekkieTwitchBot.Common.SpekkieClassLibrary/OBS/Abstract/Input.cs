using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Abstract;

public class Input
{
    protected Input(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    protected Input()
    {
    }

    [JsonProperty(PropertyName = "inputName")]
    public string InputName { get; set; } = null!;

    [JsonProperty(PropertyName = "inputKind")]
    public string InputKind { get; set; } = null!;
}