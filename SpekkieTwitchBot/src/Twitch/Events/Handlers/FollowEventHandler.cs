using SpekkieTwitchBot.Twitch.FileHandling;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class FollowEventHandler
{
    private readonly TwitchFileWriter _TwitchFileWriter;
    private readonly TwitchFileReader _TwitchFileReader;

    public FollowEventHandler(TwitchFileWriter twitchFileWriter, TwitchFileReader twitchFileReader)
    {
        _TwitchFileReader = twitchFileReader;
        _TwitchFileWriter = twitchFileWriter;
    }

    public void HandleFollow(object? sender, OnFollowArgs e)
    {
        string followerName = e.DisplayName;
        string mostRecentFollower = _TwitchFileReader.ReadMostRecentFollowerFile();
        if (mostRecentFollower.Equals(followerName)) return;

        _TwitchFileWriter.WriteMostRecentFollowerFile(followerName);
    }

    private async void GetTotalFollowers()
    {
        
    }
}