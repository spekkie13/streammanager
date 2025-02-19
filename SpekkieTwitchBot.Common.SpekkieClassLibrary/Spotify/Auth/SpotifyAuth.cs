#nullable disable
using Newtonsoft.Json;

namespace SpekkieClassLibrary.Spotify.Auth;

public class SpotifyAuth
{
    [JsonProperty(PropertyName = "client_id")]
    public string ClientId { get; set; }

    [JsonProperty(PropertyName = "client_secret")]
    public string ClientSecret { get; set; }

    [JsonProperty(PropertyName = "token")] 
    public string Token { get; set; }

    [JsonProperty(PropertyName = "refresh_token")]
    public string RefreshToken { get; set; }
    
    public string Code { get; set; }
}