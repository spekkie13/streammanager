using SpekkieClassLibrary.Twitch.Pubsub.Types;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class ChannelSubscriptionArgs : EventArgs
{
    public ChannelSubscription Subscription;
    public string ChannelId;
}