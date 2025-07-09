using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class ChannelPointsRewardRedeemedArgs : EventArgs
{
    public string? ChannelId { get; set; }
    public RewardRedeemed? RewardRedeemed { get; set; }
}