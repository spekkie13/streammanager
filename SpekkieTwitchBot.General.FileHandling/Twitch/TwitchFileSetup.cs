using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileSetup
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileSetup _fileSetup;

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
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        var file = $"{dir}RecentFollower.txt";

        var dirExists = _fileSetup.DirExists(dir);
        if (!dirExists)
            _fileSetup.CreateDir(dir);

        var fileExists = _fileSetup.FileExists(file);
        if (!fileExists)
            _fileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupRecentSubFile()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        var file = $"{dir}RecentSub.txt";

        var dirExists = _fileSetup.DirExists(dir);
        if (!dirExists)
            _fileSetup.CreateDir(dir);

        var fileExists = _fileSetup.FileExists(file);
        if (!fileExists)
            _fileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupFollowerGoalFile()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        var file = $"{dir}FollowerGoal.txt";

        var dirExists = _fileSetup.DirExists(dir);
        if (!dirExists)
            _fileSetup.CreateDir(dir);

        var fileExists = _fileSetup.FileExists(file);
        if (!fileExists)
            _fileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }

    private void SetupSubGoalFile()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        var file = $"{dir}SubGoal.txt";

        var dirExists = _fileSetup.DirExists(dir);
        if (!dirExists)
            _fileSetup.CreateDir(dir);

        var fileExists = _fileSetup.FileExists(file);
        if (!fileExists)
            _fileSetup.CreateFile(file);

        File.WriteAllText(file, "");
    }
}