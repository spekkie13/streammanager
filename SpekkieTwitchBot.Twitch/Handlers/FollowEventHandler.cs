using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.Follower;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Handlers;

public class FollowEventHandler(
    TwitchFileWriter twitchFileWriter,
    TwitchFileReader twitchFileReader,
    CustomTwitchHttpClient client)
{
    public void HandleFollow(object? sender, OnFollowArgs e)
    {
        var followerName = e.DisplayName;
        var mostRecentFollower = twitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        UpdateFollowerInfo();
    }

    private async void UpdateFollowerInfo()
    {
        var url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        var message = await client.GetAsync(url);

        var response = await message.Content.ReadAsStringAsync();
        var req = JsonConvert.DeserializeObject<FollowerRequest>(response);
        twitchFileWriter.WriteTotalFollowersFile(req?.Total.ToString() ?? "0");
        twitchFileWriter.WriteMostRecentFollowerFile(req?.Data?[0].UserName ?? "N/A");
    }
}