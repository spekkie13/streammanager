using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileReader
{
    private readonly FileReader _FileReader;
    
    public SpotifyFileReader(FileReader fileReader)
    {
        _FileReader = fileReader;
    }
    
    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public string ReadSpotifyAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";
        string jsonData = _FileReader.Read(dir);

        return jsonData;
    }
}