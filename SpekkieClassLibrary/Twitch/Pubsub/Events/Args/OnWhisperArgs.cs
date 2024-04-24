using SpekkieClassLibrary.Twitch.Pubsub.EventData;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class OnWhisperArgs : EventArgs
{
    public Whisper Whisper;
    public string ChannelId;
}