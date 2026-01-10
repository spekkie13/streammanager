namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub.Models;

public class PubSubPayload
{
    public required string type { get; set; }
    public required string nonce { get; set; }
    public required object data { get; set; }
}