using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using TwitchAuthService.Interfaces;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Handlers;

public class FollowEventHandler(
    ITwitchFileReader twitchFileReader,
    ITwitchFileWriter twitchFileWriter,
    ICustomTwitchHttpClient client)
{
    public void HandleFollowAsync(object? sender, OnFollowArgs e)
    {
        if (string.IsNullOrEmpty(e.DisplayName))
            return;
        
        _ = ProcessFollowAsync(sender, e);
    }
    
    public async Task ProcessFollowAsync(object? sender, OnFollowArgs e)
    {
        string followerName = e.DisplayName;
        try
        {
            string mostRecentFollower = await twitchFileReader.ReadMostRecentFollowerFileAsync();
            if (mostRecentFollower.Equals(followerName)) 
                return;

            int followerCount = await client.GetFollowerCount();
            await twitchFileWriter.WriteMostRecentFollowerFileAsync(followerName);
            await twitchFileWriter.WriteTotalFollowersFileAsync(followerCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
