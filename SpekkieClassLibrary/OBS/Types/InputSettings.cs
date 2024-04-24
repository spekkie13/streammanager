using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Abstract;

namespace SpekkieClassLibrary.OBS.Types;

public class InputSettings : Input
{
    [JsonProperty(PropertyName = "inputSettings")]
    public JObject? Settings { set; get; }

    public InputSettings(JObject data) : base(data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public InputSettings() { }
}