using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Twitch.FileHandling;

public class TwitchFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Twitch";

    public TwitchFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
        SetupRecentFollowerFile();
        SetupRecentSubFile();
        SetupFollowerGoalFile();
        SetupRecentSubFile();
        SetupSubGoalFile();
    }

    private void SetupRecentFollowerFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}RecentFollower.txt";
        
        bool dirExists = _fileSetup.DirExists(dir);
        if(!dirExists)
            _fileSetup.CreateDir(dir);

        bool fileExists = _fileSetup.FileExists(file);
        if(!fileExists)
            _fileSetup.CreateFile(file);
    }
    
    private void SetupRecentSubFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}RecentSub.txt";
        
        bool dirExists = _fileSetup.DirExists(dir);
        if(!dirExists)
            _fileSetup.CreateDir(dir);

        bool fileExists = _fileSetup.FileExists(file);
        if(!fileExists)
            _fileSetup.CreateFile(file);
    }
    
    private void SetupFollowerGoalFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}FollowerGoal.txt";
        
        bool dirExists = _fileSetup.DirExists(dir);
        if(!dirExists)
            _fileSetup.CreateDir(dir);

        bool fileExists = _fileSetup.FileExists(file);
        if(!fileExists)
            _fileSetup.CreateFile(file);
    }
    
    private void SetupSubGoalFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}SubGoal.txt";
        
        bool dirExists = _fileSetup.DirExists(dir);
        if(!dirExists)
            _fileSetup.CreateDir(dir);

        bool fileExists = _fileSetup.FileExists(file);
        if(!fileExists)
            _fileSetup.CreateFile(file);
    }
}