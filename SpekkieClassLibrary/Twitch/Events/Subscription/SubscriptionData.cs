namespace SpekkieClassLibrary.Twitch.Events.Subscription;

public class SubscriptionData
{
    public string? BroadcasterId { get; set; }
    public string? BroadcasterLogin { get; set; }
    public string? BroadcasterName { get; set; }
    public string? GifterId { get; set; }
    public string? GifterLogin { get; set; }
    public string? GifterName { get; set; }
    public bool IsGift { get; set; }
    public string? Tier { get; set; }
    public string? PlanName { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserLogin { get; set; }
}