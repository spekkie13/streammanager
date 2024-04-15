namespace SpekkieTwitchBot.Models.Twitch;

public class FollowerRequest
{
    public int Total { get; set; }
    public FollowerData[] Data { get; set; }
}