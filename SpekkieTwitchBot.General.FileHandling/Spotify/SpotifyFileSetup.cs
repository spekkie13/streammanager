using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileSetup
{
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                             $"{Path.DirectorySeparatorChar}SpekkieTwitchBot";

    private readonly FileSetup _fileSetup;
    private readonly string _OutputDir = $"{Path.DirectorySeparatorChar}Output{Path.DirectorySeparatorChar}Spotify";

    public SpotifyFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupSongFiles();
    }

    public void SetupSongFiles()
    {
        var titleDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        var artistDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        var pictureDir = $"{BaseDir}{_OutputDir}{Path.DirectorySeparatorChar}currentSong.png";

        if (!_fileSetup.FileExists(titleDir))
            _fileSetup.CreateFile(titleDir);

        if (!_fileSetup.FileExists(artistDir))
            _fileSetup.CreateFile(artistDir);

        if (!_fileSetup.FileExists(pictureDir))
            _fileSetup.CreateFile(pictureDir);
    }
}