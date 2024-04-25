using SpekkieClassLibrary.Twitch.Pubsub.Types;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Args;

public class ListenResponseArgs : EventArgs
{
    public string Topic;
    public Response Response;
    public bool Successful;
    public string ChannelId;
}