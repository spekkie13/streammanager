#nullable disable
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace SpekkieTwitchBot.Models.Twitch.Pubsub.Args;

public class OnChannelPointsRewardRedeemedArgs : EventArgs
{ 
    public string ChannelId { get; set; }
    public RewardRedeemed RewardRedeemed { get; set; }
}