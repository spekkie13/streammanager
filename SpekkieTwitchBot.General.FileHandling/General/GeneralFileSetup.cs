using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileSetup
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileSetup _fileSetup;

    public GeneralFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupCounterFiles();
        SetupLogFile();
    }

    private void SetupCounterFiles()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        var file = $"{dir}/afgeleid.txt";
        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }

    private void SetupLogFile()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log";
        var file = $"{dir}/log.txt";

        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }
}