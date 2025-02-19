using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.Follower;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Handlers;

public class FollowEventHandler
{
    private readonly TwitchFileReader _TwitchFileReader;
    private readonly TwitchFileWriter _TwitchFileWriter;
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
        var followerName = e.DisplayName;
        var mostRecentFollower = _TwitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        UpdateFollowerInfo();
    }

    private async void UpdateFollowerInfo()
    {
        var url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        var message = await _TwitchHttpClient.GetAsync(url);

        var response = await message.Content.ReadAsStringAsync();
        var req = JsonConvert.DeserializeObject<FollowerRequest>(response);
        _TwitchFileWriter.WriteTotalFollowersFile(req?.Total.ToString() ?? "0");
        _TwitchFileWriter.WriteMostRecentFollowerFile(req?.Data?[0].UserName ?? "N/A");
    }
}