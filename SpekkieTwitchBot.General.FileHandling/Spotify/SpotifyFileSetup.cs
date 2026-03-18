using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileSetup
{
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                             $"{Path.DirectorySeparatorChar}SpekkieTwitchBot";

    private readonly FileSetup _FileSetup;
    private readonly string _OutputDir = $"{Path.DirectorySeparatorChar}Output{Path.DirectorySeparatorChar}Spotify";

    public SpotifyFileSetup(FileSetup fileSetup)
    {
        _FileSetup = fileSetup;
        SetupSongFiles();
    }

    public void SetupSongFiles()
    {
        string titleDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        string pictureDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentSong.png";
        string htmlDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}nowplaying.html";

        if (!_FileSetup.FileExists(titleDir))
            _FileSetup.CreateFile(titleDir);

        if (!_FileSetup.FileExists(artistDir))
            _FileSetup.CreateFile(artistDir);

        if (!_FileSetup.FileExists(pictureDir))
            _FileSetup.CreateFile(pictureDir);

        if (!_FileSetup.FileExists(htmlDir))
            _FileSetup.CreateFile(htmlDir);
    }
}