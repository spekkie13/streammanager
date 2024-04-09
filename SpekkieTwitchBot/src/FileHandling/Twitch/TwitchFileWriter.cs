using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling.Twitch;

public class TwitchFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public TwitchFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }
    
    public void WriteTwitchAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch.json";
        _fileWriter.Write(dir, text);
    }
}