namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class BitsReceivedV2Args
{
    public string? UserName { get; init; }
    public string? ChannelName { get; init; }
    public string? UserId { get; set; }
    public string? ChannelId { get; set; }
    public DateTime Time { get; set; }
    public string? ChatMessage { get; set; }
    public int BitsUsed { get; init; }
    public int TotalBitsUsed { get; init; }
    public bool IsAnonymous { get; set; }
    public string? Context { get; set; }
}