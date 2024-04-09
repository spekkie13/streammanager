namespace SpekkieTwitchBot.FileHandling.Spotify;

public class SpotifyFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public SpotifyFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupSongFiles();
    }
    
    private void SetupSongFiles()
    {
        string titleDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        string pictureDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentSong.png";

        if (_fileSetup.FileExists(titleDir))
            _fileSetup.CreateFile(titleDir);

        if (_fileSetup.FileExists(artistDir))
            _fileSetup.CreateFile(artistDir);        
        
        if (_fileSetup.FileExists(pictureDir))
            _fileSetup.CreateFile(pictureDir);
    }
}