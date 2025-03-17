#nullable disable
namespace SpekkieClassLibrary.Twitch.Events.Subscription;

public class SubscriptionRequest
{
    public Subscription[] Data { get; set; }
    public int Total { get; set; }
    public int Points { get; set; }
}