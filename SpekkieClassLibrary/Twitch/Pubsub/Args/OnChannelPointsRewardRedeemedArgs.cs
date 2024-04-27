using SpekkieClassLibrary.Twitch.Pubsub.Types;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class ChannelPointsRewardRedeemedArgs : EventArgs
{ 
    public string ChannelId { get; set; }
    public RewardRedeemed RewardRedeemed { get; set; }
}