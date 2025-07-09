#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.EventData;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class WhisperArgs : EventArgs
{
    public string ChannelId;
    public Whisper Whisper;
}