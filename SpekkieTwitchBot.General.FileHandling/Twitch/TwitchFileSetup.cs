using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileSetup
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileSetup _FileSetup;

    public TwitchFileSetup(FileSetup fileSetup)
    {
        _FileSetup = fileSetup;
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

        bool dirExists = _FileSetup.DirExists(dir);
        if (!dirExists)
            _FileSetup.CreateDir(dir);

        bool fileExists = _FileSetup.FileExists(file);
        if (!fileExists)
            _FileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupRecentSubFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}RecentSub.txt";

        bool dirExists = _FileSetup.DirExists(dir);
        if (!dirExists)
            _FileSetup.CreateDir(dir);

        bool fileExists = _FileSetup.FileExists(file);
        if (!fileExists)
            _FileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupFollowerGoalFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}FollowerGoal.txt";

        bool dirExists = _FileSetup.DirExists(dir);
        if (!dirExists)
            _FileSetup.CreateDir(dir);

        bool fileExists = _FileSetup.FileExists(file);
        if (!fileExists)
            _FileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupSubGoalFile()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}SubGoal.txt";

        bool dirExists = _FileSetup.DirExists(dir);
        if (!dirExists)
            _FileSetup.CreateDir(dir);

        bool fileExists = _FileSetup.FileExists(file);
        if (!fileExists)
            _FileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }
}