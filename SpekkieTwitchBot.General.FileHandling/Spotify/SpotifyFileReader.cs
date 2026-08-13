using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileReader
{
    private readonly FileReader _FileReader;
    
    public SpotifyFileReader(FileReader fileReader)
    {
        _FileReader = fileReader;
    }
    
    private static readonly string BaseDir = BotPaths.BaseDir;

    public static string SpotifyAuthFilePath =>
        $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";

    public string ReadSpotifyAuthFile()
    {
        string jsonData = _FileReader.Read(SpotifyAuthFilePath);

        return jsonData;
    }

    // Lets callers notice an out-of-band re-auth (Tools/Reauth-Spotify.ps1 rewriting the file)
    // without re-reading and re-parsing it on every poll.
    public static DateTime GetSpotifyAuthLastWriteUtc()
    {
        string path = SpotifyAuthFilePath;
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
    }
}