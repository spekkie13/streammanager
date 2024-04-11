using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Spotify.FileHandling;

public class SpotifyFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Spotify";

    public SpotifyFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupSongFiles();
    }
    
    private void SetupSongFiles()
    {
        string titleDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        string pictureDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentSong.png";

        if (_fileSetup.FileExists(titleDir))
            _fileSetup.CreateFile(titleDir);

        if (_fileSetup.FileExists(artistDir))
            _fileSetup.CreateFile(artistDir);        
        
        if (_fileSetup.FileExists(pictureDir))
            _fileSetup.CreateFile(pictureDir);
    }
}