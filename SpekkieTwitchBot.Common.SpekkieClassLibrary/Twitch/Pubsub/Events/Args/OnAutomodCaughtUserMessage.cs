#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class AutomodCaughtUserMessage
{
    public AutomodCaughtResponseMessage AutomodCaughtMessage;
    public string ChannelId;
    public string UserId;
}