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
        SetupStreamStatsConfig();
        SetupTimedMessagesConfig();
        SetupFeaturesConfig();
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
                    "current": 0,
                    "endDate": "2026-12-31",
                    "tiers": [
                      { "goal": 25, "rewardEn": "first reward in English", "rewardNl": "eerste beloning in het Nederlands" },
                      { "goal": 50, "rewardEn": "second reward in English", "rewardNl": "tweede beloning in het Nederlands" },
                      { "goal": 100, "rewardEn": "third reward in English", "rewardNl": "derde beloning in het Nederlands" }
                    ]
                  }
                }
                """);
        }
    }

    private void SetupStreamStatsConfig()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}";
        string file = $"{dir}streamstats.json";

        if (!_FileSetup.FileExists(file))
        {
            using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sw = new(fs);
            sw.Write("""
                {
                  "apiUrl": "https://your-app.vercel.app",
                  "apiKey": "your-api-key-from-the-dashboard"
                }
                """);
        }
    }

    private void SetupTimedMessagesConfig()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}";
        string file = $"{dir}TimedMessages.json";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (!_FileSetup.FileExists(file))
        {
            using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sw = new(fs);
            sw.Write("""
                {
                  "TimedMessages": [
                    {
                      "Message": "Example timed message — edit TimedMessages.json to configure",
                      "IntervalMinutes": 30
                    }
                  ]
                }
                """);
        }
    }

    private void SetupFeaturesConfig()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}";
        string file = $"{dir}features.json";

        if (!_FileSetup.DirExists(dir))
            _FileSetup.CreateDir(dir);

        if (_FileSetup.FileExists(file)) return;
        
        using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter sw = new(fs);
        sw.Write("""
                 {
                   "Marathon": true
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
        {
            using FileStream fs = new(file, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
        }
    }
}
