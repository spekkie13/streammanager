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
        Task.Run(async () => await _TwitchHttpClient.GetSubscriberCount());
    }
    
    public void HandleSubAsync(object? sender, ChannelSubscriptionArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                string? subscriberName = e.Subscription?.DisplayName;
                string mostRecentSubscriber = await _TwitchFileReader.ReadMostRecentSubFileAsync();
                if (mostRecentSubscriber.Equals(subscriberName)) return;

                int subscriberCount = _TwitchHttpClient.GetSubscriberCount().Result;

                if (e.Subscription?.DisplayName == null) return;
                await _TwitchFileWriter.WriteMostRecentSubscriberFileAsync(e.Subscription.DisplayName);
                await _TwitchFileWriter.WriteTotalSubscribersFileAsync(subscriberCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        });
    }
}