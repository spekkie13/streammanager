using Newtonsoft.Json;

namespace SpekkieClassLibrary.Twitch.Auth;

public class GeneralTwitchAuth
{
    [JsonProperty(PropertyName = "BotName")]
    public string? BotName { get; set; }

    [JsonProperty(PropertyName = "BroadcasterName")]
    public string? BroadcasterName { get; set; }

    [JsonProperty(PropertyName = "ChannelId")]
    public string? ChannelId { get; set; }

    [JsonProperty(PropertyName = "Obs_Url")]
    public string? ObsUrl { get; set; }

    [JsonProperty(PropertyName = "Password")]
    public string? Password { get; set; }

    [JsonProperty(PropertyName = "Implicit_OAuth")]
    public string? ImplicitOAuth { get; set; }
    
    public static GeneralTwitchAuth Empty => new GeneralTwitchAuth();
}