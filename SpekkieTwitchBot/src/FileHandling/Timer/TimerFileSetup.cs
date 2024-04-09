namespace SpekkieTwitchBot.FileHandling.Timer;

public class TimerFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public TimerFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupTimerFile();
    }
    
    private void SetupTimerFile()
    {
        string dir = BaseDir + $"{Path.DirectorySeparatorChar}Timer";
        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        string file = $"{dir}/timer.txt";
        
        if(!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }
}