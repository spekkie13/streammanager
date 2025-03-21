using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileWriter(FileWriter fileWriter)
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public void WriteTwitchUserAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        fileWriter.Write(dir, text);
    }

    public virtual void WriteMostRecentFollowerFile(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";

        fileWriter.Write(dir, text);
    }

    public virtual void WriteTotalFollowersFile(int totalFollowers)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalFollowers.txt";
        fileWriter.Write(dir, totalFollowers.ToString());
    }

    public void WriteMostRecentSubscriberFile(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSubscriber.txt";

        fileWriter.Write(dir, text);
    }

    public void WriteTotalSubscribersFile(int totalSubscribers)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalSubscribers.txt";
        fileWriter.Write(dir, totalSubscribers.ToString());
    }
}