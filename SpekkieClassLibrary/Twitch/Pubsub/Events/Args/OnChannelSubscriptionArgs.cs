using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class OnChannelSubscriptionArgs : EventArgs
{
    public ChannelSubscription Subscription;
    public string ChannelId;
}