namespace SpekkieTwitchBot.Models.Twitch.Auth;

public class TwitchUserAuth
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string UserToken { get; set; }
    public string Code { get; set; }
    public string UserRefreshToken { get; set; }
}