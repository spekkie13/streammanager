using SpekkieClassLibrary.Twitch.Pubsub.EventData;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class WhisperArgs : EventArgs
{
    public Whisper Whisper;
    public string ChannelId;
}