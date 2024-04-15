using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Twitch.FileHandling;
using SpekkieTwitchBot.Web;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class FollowEventHandler
{
    private readonly TwitchFileWriter _TwitchFileWriter;
    private readonly TwitchFileReader _TwitchFileReader;
    private readonly CustomTwitchHttpClient _TwitchHttpClient;
    
    public FollowEventHandler(
        TwitchFileWriter twitchFileWriter, 
        TwitchFileReader twitchFileReader,
        CustomTwitchHttpClient client)
    {
        _TwitchFileReader = twitchFileReader;
        _TwitchFileWriter = twitchFileWriter;
        _TwitchHttpClient = client;
    }

    public void HandleFollow(object? sender, OnFollowArgs e)
    {
        
        string followerName = e.DisplayName;
        string mostRecentFollower = _TwitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;
        
        UpdateFollowerInfo();
    }

    private async void UpdateFollowerInfo()
    {
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        FollowerRequest? req = JsonConvert.DeserializeObject<FollowerRequest>(response);
        _TwitchFileWriter.WriteTotalFollowersFile(req?.Total.ToString() ?? "0");
        _TwitchFileWriter.WriteMostRecentFollowerFile(req?.Data[0].user_name ?? "N/A");
    }
}