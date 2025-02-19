#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.EventData;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class WhisperArgs : EventArgs
{
    public string ChannelId;
    public Whisper Whisper;
}