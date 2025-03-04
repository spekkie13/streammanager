using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileReader(FileReader fileReader)
{
    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public string ReadSpotifyAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";
        string jsonData = fileReader.Read(dir);

        return jsonData;
    }
}