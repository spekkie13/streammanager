using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Handlers;

public class FollowEventHandler(
    TwitchFileReader twitchFileReader,
    TwitchFileWriter twitchFileWriter,
    CustomTwitchHttpClient client)
{
    public void HandleFollow(object? sender, OnFollowArgs e)
    {
        string followerName = e.DisplayName;
        string mostRecentFollower = twitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        int followerCount = client.UpdateFollowerInfo().Result;
        twitchFileWriter.WriteMostRecentFollowerFile(e.DisplayName);
        twitchFileWriter.WriteTotalFollowersFile(followerCount);
    }
}
