using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Spotify.FileHandling;

public class SpotifyFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"{Path.DirectorySeparatorChar}SpekkieTwitchBot";
    private readonly string _OutputDir = $"{Path.DirectorySeparatorChar}Output{Path.DirectorySeparatorChar}Spotify";

    public SpotifyFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupSongFiles();
    }
    
    public void SetupSongFiles()
    {
        string titleDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        string pictureDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentSong.png";

        if (!_fileSetup.FileExists(titleDir))
            _fileSetup.CreateFile(titleDir);

        if (!_fileSetup.FileExists(artistDir))
            _fileSetup.CreateFile(artistDir);        
        
        if (!_fileSetup.FileExists(pictureDir))
            _fileSetup.CreateFile(pictureDir);
    }
}