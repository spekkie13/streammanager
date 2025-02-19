using Newtonsoft.Json;

namespace SpekkieClassLibrary.Spotify;

public class TokenResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = "";

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = "";
}