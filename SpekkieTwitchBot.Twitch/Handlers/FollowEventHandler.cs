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
        string followerName = e.DisplayName;
        string mostRecentFollower = twitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        UpdateFollowerInfo().Wait(); // Ensure we can test it
    }

    private async Task UpdateFollowerInfo()  // Change to Task
    {
        string url = $"{TwitchConstants.TwitchFollowersUrl}?broadcaster_id={TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await client.GetAsync(url);

        string response = await message.Content.ReadAsStringAsync();
        FollowerRequest? req = JsonConvert.DeserializeObject<FollowerRequest>(response);
        twitchFileWriter.WriteTotalFollowersFile(req?.Total.ToString() ?? "0");
        twitchFileWriter.WriteMostRecentFollowerFile(req?.Data?[0].UserName ?? "N/A");
    }
}
