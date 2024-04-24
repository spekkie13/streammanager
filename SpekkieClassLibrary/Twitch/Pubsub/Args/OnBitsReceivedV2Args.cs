namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class BitsReceivedV2Args
{
    public string? UserName { get; set; }
    public string? ChannelName { get; set; }
    public string? UserId { get; set; }
    public string? ChannelId { get; set; }
    public DateTime Time { get; set; }
    public string? ChatMessage { get; set; }
    public int BitsUsed { get; set; }
    public int TotalBitsUsed { get; set; }
    public bool IsAnonymous { get; set; }
    public string? Context { get; set; }
}