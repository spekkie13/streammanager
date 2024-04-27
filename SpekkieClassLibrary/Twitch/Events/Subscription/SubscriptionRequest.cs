#nullable disable
namespace SpekkieClassLibrary.Twitch.Events.Subscription;

public class SubscriptionRequest
{
    public SubscriptionData[] Data { get; set; }
    public int Total { get; set; }
    public int Points { get; set; }
}