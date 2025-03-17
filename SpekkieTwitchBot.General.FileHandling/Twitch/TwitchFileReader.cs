using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileReader(FileReader fileReader)
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public string ReadTwitchUserAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        string jsonData = fileReader.Read(dir);

        return jsonData;
    }

    public string ReadTwitchGeneralAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-General.json";
        string jsonData = fileReader.Read(dir);

        return jsonData;
    }

    public virtual string ReadMostRecentFollowerFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        string text = fileReader.Read(file);

        return text;
    }

    public string ReadMostRecentSubFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSub.txt";
        string text = fileReader.Read(file);

        return text;
    }

    public string ReadSubGoalFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}FollowerGoal.txt";
        string text = fileReader.Read(file);

        return text;
    }

    public string ReadFollowerGoalFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}SubGoal.txt";
        string text = fileReader.Read(file);

        return text;
    }
}