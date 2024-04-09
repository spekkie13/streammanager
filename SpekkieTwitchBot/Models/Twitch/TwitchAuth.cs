namespace SpekkieTwitchBot.Models.Twitch;

public class TwitchAuth
{
    public string BotName { get; set; }
    public string? BroadcasterName { get; set; }
    public string ChannelId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string UserToken { get; set; }
    public string AppToken { get; set; }
    public string Scopes { get; set; }
    public string Obs_Url { get; set; }
    public string Password { get; set; }
    public string OAuth { get; set; }
    public string Implicit_OAuth { get; set; }
    public string Code { get; set; }
    public string RefreshToken { get; set; }
}