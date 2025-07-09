using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class ChannelSubscriptionArgs : EventArgs
{
    public string? ChannelId;
    public ChannelSubscription? Subscription;
}