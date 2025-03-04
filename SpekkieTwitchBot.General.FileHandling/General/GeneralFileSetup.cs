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
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        string file = $"{dir}/afgeleid.txt";
        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }

    private void SetupLogFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log";
        string file = $"{dir}/log.txt";

        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }
}