using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Events.Subscription;
using SpekkieClassLibrary.Twitch.Pubsub.Events.Args;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.Twitch.FileHandling;
using SpekkieTwitchBot.Twitch.General;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class SubEventHandler
{
    private readonly TwitchFileWriter _TwitchFileWriter;
    private readonly CustomTwitchHttpClient _TwitchHttpClient;

    public SubEventHandler(TwitchFileWriter twitchFileWriter, CustomTwitchHttpClient client)
    {
        _TwitchHttpClient = client;
        _TwitchFileWriter = twitchFileWriter;
        UpdateSubscriberInfo();
    }

    public void HandleSub(object? sender, ChannelSubscriptionArgs e)
    {
        UpdateSubscriberInfo();
    }
    
    private async void UpdateSubscriberInfo()
    {
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        SubscriptionRequest? req = JsonConvert.DeserializeObject<SubscriptionRequest>(response);
        _TwitchFileWriter.WriteTotalSubscribersFile(req?.Total.ToString() ?? "0");
        _TwitchFileWriter.WriteMostRecentSubscriberFile(req?.Data?[0].UserName ?? "N/A");
    }
}