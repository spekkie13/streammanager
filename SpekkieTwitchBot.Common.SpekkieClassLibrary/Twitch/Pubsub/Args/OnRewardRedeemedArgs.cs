#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class RewardRedeemedArgs : EventArgs
{
    public string ChannelId;
    public string DisplayName;
    public string Login;
    public string Message;
    public Guid RedemptionId;
    public int RewardCost;
    public Guid RewardId;
    public string RewardPrompt;
    public string RewardTitle;
    public string Status;
    public DateTime TimeStamp;
}