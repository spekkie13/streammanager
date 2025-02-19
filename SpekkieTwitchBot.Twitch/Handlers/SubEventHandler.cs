using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.Subscription;
using SpekkieClassLibrary.Twitch.Pubsub.Events.Args;
using SpekkieTwitchBot.General.FileHandling.Twitch;

namespace TwitchAuthService.Handlers;

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
        var url = $"{TwitchConstants.TwitchSubscribersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        var message = await _TwitchHttpClient.GetAsync(url);

        var response = await message.Content.ReadAsStringAsync();
        var req = JsonConvert.DeserializeObject<SubscriptionRequest>(response);
        _TwitchFileWriter.WriteTotalSubscribersFile(req?.Total.ToString() ?? "0");
        _TwitchFileWriter.WriteMostRecentSubscriberFile(req?.Data?[0].UserName ?? "N/A");
    }
}