#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class ListenResponseArgs : EventArgs
{
    public string ChannelId;
    public Response Response;
    public bool Successful;
    public string Topic;
}