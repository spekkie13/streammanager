using Newtonsoft.Json;

namespace SpekkieTwitchBot.Systems.Twitch.Models.Auth;

public class AuthorizationCredentials
{
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; set; }

    [JsonProperty(PropertyName = "expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty(PropertyName = "refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty(PropertyName = "scope")] 
    public List<string> Scope { get; set; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; set; }
    
    public static AuthorizationCredentials Empty => new();
}