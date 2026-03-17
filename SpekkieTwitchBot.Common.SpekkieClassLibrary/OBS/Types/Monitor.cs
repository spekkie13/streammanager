using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Types;

public class Monitor
{
    public Monitor(JObject data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public Monitor()
    {
    }

    [JsonProperty(PropertyName = "monitorHeight")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "monitorWidth")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "monitorName")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "monitorIndex")]
    public int Index { get; set; }

    [JsonProperty(PropertyName = "monitorPositionX")]
    public int PositionX { get; set; }

    [JsonProperty(PropertyName = "monitorPositionY")]
    public int PositionY { get; set; }
}