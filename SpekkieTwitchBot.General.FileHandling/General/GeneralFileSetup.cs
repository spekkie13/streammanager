using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileSetup
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileSetup _FileSetup;

    public GeneralFileSetup(FileSetup fileSetup)
    {
        _FileSetup = fileSetup;
        SetupCounterFiles();
        SetupLogFile();
    }

    private void SetupCounterFiles()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        string file = $"{dir}/afgeleid.txt";
        if (!_FileSetup.FileExists(file))
            _FileSetup.CreateFile(file);
    }

    private void SetupLogFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log";
        string file = $"{dir}/log.txt";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (!_FileSetup.FileExists(file))
            _FileSetup.CreateFile(file);
    }
}