using SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;
using SpekkieTwitchBot.General.FileHandling.Twitch;

namespace TwitchAuthService.Handlers;

public class SubEventHandler
{
    private readonly CustomTwitchHttpClient _TwitchHttpClient;
    private readonly TwitchFileWriter _TwitchFileWriter;
    private readonly TwitchFileReader _TwitchFileReader;

    public SubEventHandler(CustomTwitchHttpClient client, TwitchFileWriter twitchFileWriter, TwitchFileReader twitchFileReader)
    {
        _TwitchHttpClient = client;
        _TwitchFileWriter = twitchFileWriter;
        _TwitchFileReader = twitchFileReader;
        _TwitchHttpClient.GetSubscriberCount().Wait();
    }
    
    public void HandleSub(object? sender, ChannelSubscriptionArgs e)
    {
        string? subscriberName = e.Subscription?.DisplayName;
        string mostRecentSubscriber = _TwitchFileReader.ReadMostRecentSubFile();
        if (mostRecentSubscriber.Equals(subscriberName)) return;

        int subscriberCount = _TwitchHttpClient.GetSubscriberCount().Result;

        if (e.Subscription?.DisplayName == null) return;
        _TwitchFileWriter.WriteMostRecentSubscriberFile(e.Subscription.DisplayName);
        _TwitchFileWriter.WriteTotalSubscribersFile(subscriberCount);
    }
}