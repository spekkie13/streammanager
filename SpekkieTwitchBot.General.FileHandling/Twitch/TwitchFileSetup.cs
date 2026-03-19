using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileSetup
{
    private const string OutputDir = "/Output/Twitch";

    private static readonly string BaseDir = BotPaths.BaseDir;

    private readonly FileSetup _FileSetup;

    public TwitchFileSetup(FileSetup fileSetup)
    {
        _FileSetup = fileSetup;
        SetupFile("RecentFollower.txt", clearOnBoot: true);
        SetupFile("RecentSubscriber.txt", clearOnBoot: true);
        SetupFile("LatestSubDisplay.txt", clearOnBoot: false);
        SetupFile("latestactivity.html", clearOnBoot: false);
        SetupFile("subgoal.html", clearOnBoot: false);
        SetupGoalsConfig();
    }

    private void SetupGoalsConfig()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}";
        string file = $"{dir}goals.json";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (!_FileSetup.FileExists(file))
        {
            using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sw = new(fs);
            sw.Write("""
                {
                  "followerGoal": 1000,
                  "subGoal": {
                    "goal": 50,
                    "current": 0,
                    "rewardEn": "describe your reward here in English",
                    "rewardNl": "beschrijf je beloning hier in het Nederlands",
                    "endDate": "2026-12-31"
                  }
                }
                """);
        }
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
        {
            using FileStream fs = new(file, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
        }
    }
}
