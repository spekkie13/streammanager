namespace SpekkieClassLibrary.Overlay;

public class OverlaySupport
{
    public string LatestFollower { get; init; } = "";
    public int FollowerCount { get; init; }
    public string LatestSub { get; init; } = "";
    public int SubscriberCount { get; init; }
    // Cumulative bits cheered and euros donated across the channel (running totals, no goal/perk).
    public int BitsTotal { get; init; }
    public decimal DonationTotal { get; init; }
    // The active sub goal (next unreached milestone) and its reward, for the overlay to display.
    public OverlaySubGoal SubGoal { get; init; } = new();
    public OverlayNowPlaying NowPlaying { get; init; } = new();
    public List<string> Socials { get; init; } = [];
}
