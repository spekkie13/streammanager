using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class OnAutomodCaughtMessageArgs
{
    public AutomodCaughtMessage AutomodCaughtMessage;
    public string ChannelId;
}