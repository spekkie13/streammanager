namespace SpekkieTwitchBot.FileHandling.General;

public class GeneralFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public GeneralFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupCounterFiles();
        SetupLogFile();
    }
    
    private void SetupCounterFiles()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        string file = $"{dir}/afgeleid.txt";
        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }

    private void SetupLogFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Log";
        string file = $"{dir}/log.txt";
        
        if (!_fileSetup.DirExists(dir))
            _fileSetup.CreateDir(dir);

        if (!_fileSetup.FileExists(file))
            _fileSetup.CreateFile(file);
    }
}