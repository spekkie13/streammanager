using Newtonsoft.Json;

namespace SpekkieClassLibrary.OBS.Types;

public class GetProfileListInfo
{
    [JsonProperty(PropertyName = "currentProfileName")]
    public string? CurrentProfileName { set; get; }

    [JsonProperty(PropertyName = "profiles")]
    public List<string>? Profiles { set; get; }
}