namespace SpekkieTwitchBot.FileHandling.Spotify;

public class SpotifyFileReader
{
    private readonly FileReader _fileReader;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    
    public SpotifyFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }
    
    public string ReadSpotifyAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";
        string jsonData = _fileReader.Read(dir);

        return jsonData;
    }
}