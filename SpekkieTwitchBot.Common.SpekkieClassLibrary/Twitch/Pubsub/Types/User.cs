#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class User
{
    [JsonProperty(PropertyName = "id")] 
    public string Id { get; set; }

    [JsonProperty(PropertyName = "login")] 
    public string Login { get; set; }

    [JsonProperty(PropertyName = "display_name")]
    public string DisplayName { get; set; }
}