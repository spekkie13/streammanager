using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileReader
{
    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileReader _fileReader;

    public SpotifyFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }

    public string ReadSpotifyAuthFile()
    {
        var dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";
        var jsonData = _fileReader.Read(dir);

        return jsonData;
    }
}