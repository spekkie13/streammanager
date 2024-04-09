using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling.Twitch;

public class TwitchFileReader
{
    private readonly FileReader _fileReader;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    
    public TwitchFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }
    
    public static string ReadTwitchAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch.json";
        string jsonData = File.ReadAllText(dir);

        return jsonData;
    }
}