namespace SpekkieClassLibrary.Overlay;

public class OverlaySupport
{
    public string LatestFollower { get; init; } = "";
    public int FollowerCount { get; init; }
    public string LatestSub { get; init; } = "";
    public int SubscriberCount { get; init; }
    public OverlayNowPlaying NowPlaying { get; init; } = new();
    public List<string> Socials { get; init; } = [];
}
