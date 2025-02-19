#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class ChannelSubscriptionArgs : EventArgs
{
    public string ChannelId;
    public ChannelSubscription Subscription;
}