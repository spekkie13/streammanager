namespace SpekkieTwitchBot.Models.Auth;

public class SpotifyAuth
{
    public string client_id { get; set; } = "";
    public string client_secret { get; set; } = "";
    public string token { get; set; } = "";
    public string refresh_token { get; set; } = "";
}