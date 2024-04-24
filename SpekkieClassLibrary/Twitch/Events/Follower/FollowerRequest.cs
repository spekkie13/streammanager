namespace SpekkieClassLibrary.Twitch.Events.Follower;

public class FollowerRequest
{
    public int Total { get; set; }
    public FollowerData[]? Data { get; set; }
}