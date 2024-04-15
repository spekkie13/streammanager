using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.Models.Twitch.Events.Subscription;
using SpekkieTwitchBot.Twitch.FileHandling;
using SpekkieTwitchBot.Web;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages;

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

    public void HandleSub(object? sender, OnChannelSubscriptionArgs e)
    {
        UpdateSubscriberInfo();
    }
    
    private async void UpdateSubscriberInfo()
    {
        string url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        SubscriptionRequest? req = JsonConvert.DeserializeObject<SubscriptionRequest>(response);
        _TwitchFileWriter.WriteTotalSubscribersFile(req?.total.ToString() ?? "0");
        _TwitchFileWriter.WriteMostRecentSubscriberFile(req?.data[0].user_name ?? "N/A");
    }
}