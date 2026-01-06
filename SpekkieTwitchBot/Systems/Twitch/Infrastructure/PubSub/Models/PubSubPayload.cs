namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub.Models;

public class PubSubPayload
{
    public required string Type { get; set; }
    public required string Nonce { get; set; }
    public required object Data { get; set; }
}