namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub.Models;

public class PubSubPayload
{
    public string type { get; set; }
    public string nonce { get; set; }
    public object data { get; set; }
}