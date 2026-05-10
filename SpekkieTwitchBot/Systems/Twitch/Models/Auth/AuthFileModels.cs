using Newtonsoft.Json;

namespace SpekkieTwitchBot.Systems.Twitch.Models.Auth;

public sealed class TwitchGeneralFile
{
    public string? BotName { get; set; }
    public string? BroadcasterName { get; set; }
    public string? ChannelId { get; set; }

    [JsonProperty(PropertyName = "Obs_Url")]
    public string? ObsUrl { get; set; }
    public string? Password { get; set; }

    [JsonProperty(PropertyName = "Implicit_OAuth")]
    public string? ImplicitOAuth { get; set; }
}

public sealed class TwitchUserFile
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public string? UserToken { get; set; }          // Helix access token
    public string? UserRefreshToken { get; set; }   // refresh token
    public string? Code { get; set; }               // optional
}