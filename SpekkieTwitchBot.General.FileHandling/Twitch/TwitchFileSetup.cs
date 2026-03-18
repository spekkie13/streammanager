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
        SetupFile("RecentFollower.txt", clearOnBoot: true);
        SetupFile("RecentSub.txt", clearOnBoot: true);
        SetupFile("FollowerGoal.txt", clearOnBoot: true);
        SetupFile("SubGoal.txt", clearOnBoot: true);
        SetupFile("latestactivity.html", clearOnBoot: false);
        SetupFile("subgoal.html", clearOnBoot: false);
        SetupSubGoalConfig();
    }

    private void SetupSubGoalConfig()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}";
        string file = $"{dir}subgoal.json";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (!_FileSetup.FileExists(file))
            File.WriteAllText(file, """
                {
                  "goal": 25,
                  "endDate": "2026-12-31"
                }
                """);
    }

    private void SetupFile(string filename, bool clearOnBoot)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}";
        string file = $"{dir}{filename}";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (!_FileSetup.FileExists(file))
            _FileSetup.CreateFile(file);

        if (clearOnBoot)
            File.WriteAllText(file, "");
    }
}
