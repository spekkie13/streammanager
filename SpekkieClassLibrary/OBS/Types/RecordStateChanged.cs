using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable disable
namespace SpekkieClassLibrary.OBS.Types;

public class RecordStateChanged : OutputStateChanged
{
    [JsonProperty(PropertyName = "outputPath")]
    public string OutputPath { set; get; }

    public RecordStateChanged(JObject body) :base(body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public RecordStateChanged() { }
}