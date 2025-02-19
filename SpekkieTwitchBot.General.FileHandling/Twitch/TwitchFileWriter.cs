using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileWriter
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileWriter _fileWriter;

    public TwitchFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    public void WriteTwitchUserAuthFile(string text)
    {
        var dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        _fileWriter.Write(dir, text);
    }

    public void WriteMostRecentFollowerFile(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";

        _fileWriter.Write(dir, text);
    }

    public void WriteTotalFollowersFile(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalFollowers.txt";
        _fileWriter.Write(dir, text);
    }

    public void WriteMostRecentSubscriberFile(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSubscriber.txt";

        _fileWriter.Write(dir, text);
    }

    public void WriteTotalSubscribersFile(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalSubscribers.txt";
        _fileWriter.Write(dir, text);
    }
}