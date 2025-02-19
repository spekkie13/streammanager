using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileReader
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileReader _fileReader;

    public TwitchFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }

    public string ReadTwitchUserAuthFile()
    {
        var dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        var jsonData = _fileReader.Read(dir);

        return jsonData;
    }

    public string ReadTwitchGeneralAuthFile()
    {
        var dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-General.json";
        var jsonData = _fileReader.Read(dir);

        return jsonData;
    }

    public string ReadMostRecentFollowerFile()
    {
        var file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        var text = _fileReader.Read(file);

        return text;
    }

    public string ReadMostRecentSubFile()
    {
        var file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSub.txt";
        var text = _fileReader.Read(file);

        return text;
    }

    public string ReadSubGoalFile()
    {
        var file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}FollowerGoal.txt";
        var text = _fileReader.Read(file);

        return text;
    }

    public string ReadFollowerGoalFile()
    {
        var file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}SubGoal.txt";
        var text = _fileReader.Read(file);

        return text;
    }
}