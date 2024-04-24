namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class RewardRedeemedArgs : EventArgs
{
    public DateTime TimeStamp;
    public string? ChannelId;
    public string? Login;
    public string? DisplayName;
    public string? Message;
    public Guid RewardId;
    public string? RewardTitle;
    public string? RewardPrompt;
    public int RewardCost;
    public string? Status;
    public Guid RedemptionId;
}