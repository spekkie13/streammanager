#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class FilterReorderItem
{
    [JsonProperty(PropertyName = "name")] public string Name { set; get; }

    [JsonProperty(PropertyName = "type")] public string Type { set; get; }
}