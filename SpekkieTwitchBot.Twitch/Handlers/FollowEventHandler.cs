using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Handlers;

public class FollowEventHandler(
    TwitchFileReader twitchFileReader,
    CustomTwitchHttpClient client)
{
    public void HandleFollow(object? sender, OnFollowArgs e)
    {
        string followerName = e.DisplayName;
        string mostRecentFollower = twitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        client.UpdateFollowerInfo().Wait(); // Ensure we can test it
    }
}
