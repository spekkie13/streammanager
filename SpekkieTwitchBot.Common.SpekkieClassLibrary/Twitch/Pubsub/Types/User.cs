#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class User
{
    [JsonProperty(PropertyName = "id")] public string Id { get; protected set; }

    [JsonProperty(PropertyName = "login")] public string Login { get; protected set; }

    [JsonProperty(PropertyName = "display_name")]
    public string DisplayName { get; protected set; }
}