using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class OnListenResponseArgs : EventArgs
{
    public string Topic;
    public Response Response;
    public bool Successful;
    public string ChannelId;
}