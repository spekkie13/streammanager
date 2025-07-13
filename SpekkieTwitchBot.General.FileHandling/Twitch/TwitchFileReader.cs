using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileReader(FileReader fileReader) : ITwitchFileReader
{
    private const string OutputDir = "/Output/Twitch";
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public string ReadTwitchUserAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        string jsonData = fileReader.Read(dir);

        return jsonData;
    }

    public string ReadTwitchGeneralAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-General.json";
        string jsonData = fileReader.Read(dir);

        return jsonData;
    }

    public async Task<string> ReadMostRecentFollowerFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        return await fileReader.ReadAsync(file);
    }

    public async Task<string> ReadMostRecentSubFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSub.txt";
        return await fileReader.ReadAsync(file);
    }

    public async Task<string> ReadSubGoalFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}FollowerGoal.txt";
        return await fileReader.ReadAsync(file);
    }

    public async Task<string> ReadFollowerGoalFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}SubGoal.txt";
        return await fileReader.ReadAsync(file);
    }
}