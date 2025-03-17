using SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

namespace TwitchAuthService.Handlers;

public class SubEventHandler
{
    private readonly CustomTwitchHttpClient _TwitchHttpClient;

    public SubEventHandler(CustomTwitchHttpClient client)
    {
        _TwitchHttpClient = client;
        _TwitchHttpClient.UpdateSubscriberInfo().Wait();
    }

    public void HandleSub(object? sender, ChannelSubscriptionArgs e)
    {
        _TwitchHttpClient.UpdateSubscriberInfo().Wait();
    }

}