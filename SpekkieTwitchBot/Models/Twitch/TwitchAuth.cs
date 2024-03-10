namespace SpekkieTwitchBot.Models.Twitch;

public class TwitchAuth
{
    public string BotName { get; set; } = "";
    public string BroadcasterName { get; set;} = "";
    public string OAuth { get; set;} = "";
    public string Obs_url { get; set; } = "";
    public string Pubsub_url{ get; set; } = "";
    public string ChannelId{ get; set; } = "";
    public string Password { get; set; } = "";
}