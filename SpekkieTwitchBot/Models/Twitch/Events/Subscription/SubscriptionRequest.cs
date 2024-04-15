namespace SpekkieTwitchBot.Models.Twitch.Events.Subscription;

public class SubscriptionRequest
{
    public SubscriptionData[] data { get; set; }
    public int total { get; set; }
    public int points { get; set; }
}