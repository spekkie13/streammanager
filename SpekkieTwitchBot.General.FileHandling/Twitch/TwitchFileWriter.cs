using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileWriter(ITextFileWriter fileWriter) : ITwitchFileWriter
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public void WriteTwitchUserAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        fileWriter.Write(dir, text);
    }    
    
    public void WriteTwitchGeneralAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        fileWriter.Write(dir, text);
    }

    public async Task WriteMostRecentFollowerAsync(string text, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        await fileWriter.WriteAsync(dir, $"Most recent follower: {text}");
    }

    public async Task WriteTotalFollowersAsync(int totalFollowers, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalFollowers.txt";
        await fileWriter.WriteAsync(dir, totalFollowers.ToString());
    }

    public async Task WriteMostRecentSubscriberAsync(string text, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSubscriber.txt";
        await fileWriter.WriteAsync(dir, $"Most recent subscriber: {text}");
    }

    public async Task WriteTotalSubscribersAsync(int totalSubscribers, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalSubscribers.txt";
        await fileWriter.WriteAsync(dir, totalSubscribers.ToString());
    }
}