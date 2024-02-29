using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.Auth;

public class TokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = "";
}