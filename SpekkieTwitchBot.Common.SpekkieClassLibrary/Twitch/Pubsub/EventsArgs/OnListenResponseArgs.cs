using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class ListenResponseArgs : EventArgs
{
    public string? ChannelId;
    public Response? Response;
    public bool Successful;
    public string? Topic;
}