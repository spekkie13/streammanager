namespace SpekkieTwitchBot.FileHandling.Twitch;

public class TwitchFileReader
{
    private readonly FileReader _fileReader;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Twitch";
    
    public TwitchFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }
    
    public string ReadTwitchAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch.json";
        string jsonData = _fileReader.Read(dir);

        return jsonData;
    }

    public string ReadMostRecentFollowerFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        string text = _fileReader.Read(file);

        return text;
    }

    public string ReadMostRecentSubFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSub.txt";
        string text = _fileReader.Read(file);

        return text;
    }

    public string ReadSubGoalFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}FollowerGoal.txt";
        string text = _fileReader.Read(file);

        return text;
    }

    public string ReadFollowerGoalFile()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}SubGoal.txt";
        string text = _fileReader.Read(file);

        return text;
    }
}