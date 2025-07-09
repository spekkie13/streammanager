#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class AutomodCaughtUserMessage
{
    public AutomodCaughtResponseMessage AutomodCaughtMessage;
    public string ChannelId;
    public string UserId;
}