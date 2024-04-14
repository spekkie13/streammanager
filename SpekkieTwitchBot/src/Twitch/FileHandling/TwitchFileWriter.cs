using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Twitch.FileHandling;

public class TwitchFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Twitch";

    public TwitchFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }
    
    public void WriteTwitchUserAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        _fileWriter.Write(dir, text);
    }

    public void WriteMostRecentFollowerFile(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        
        _fileWriter.Write(dir, text);
    }
}