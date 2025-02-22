using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Timer;

public class TimerFileSetup
{
    private readonly FileSetup _FileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Timer";

    public TimerFileSetup(FileSetup fileSetup)
    {
        _FileSetup = fileSetup;
        SetupTimerFile();
    }

    private void SetupTimerFile()
    {
        string dir = BaseDir + OutputDir;
        if(!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        string file = $"{dir}/timer.txt";
        
        if(!_FileSetup.FileExists(file))
            _FileSetup.CreateFile(file);
    }
}