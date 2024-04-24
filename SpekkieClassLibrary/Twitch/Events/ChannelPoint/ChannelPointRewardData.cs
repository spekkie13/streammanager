namespace SpekkieClassLibrary.Twitch.Events.ChannelPoint;

public class ChannelPointRewardData
{
    public string? BroadcasterName { get; set; }
    public string? BroadcasterLogin { get; set; }
    public string? BroadcasterId { get; set; }
    public string? Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserInput { get; set; }
    public string? Status { get; set; }
    public string? RedeemedAt { get; set; }
    public Reward? Reward { get; set; }
}