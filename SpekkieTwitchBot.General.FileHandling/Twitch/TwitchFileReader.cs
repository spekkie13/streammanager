using System.Text.Json;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileReader : ITwitchFileReader
{
    private readonly FileReader _FileReader;
    
    private const string OutputDir = "/Output/Twitch";
    private static readonly string BaseDir = BotPaths.BaseDir;

    public TwitchFileReader(FileReader fileReader)
    {
        _FileReader = fileReader;
    }
    
    public string ReadTwitchUserAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        string jsonData = _FileReader.Read(dir);

        return jsonData;
    }    

    public string ReadTwitchGeneralAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-General.json";
        string jsonData = _FileReader.Read(dir);

        return jsonData;
    }

    public async Task<string> ReadMostRecentFollowerFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        return await _FileReader.ReadAsync(file);
    }

    public async Task<string> ReadMostRecentSubFileAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSubscriber.txt";
        return await _FileReader.ReadAsync(file);
    }

    public async Task<string> ReadLatestSubDisplayAsync()
    {
        string file = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}LatestSubDisplay.txt";
        return await _FileReader.ReadAsync(file);
    }

    public async Task<StreamGoalsConfig?> ReadGoalsConfigAsync()
    {
        string file = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}goals.json";
        string json = await _FileReader.ReadAsync(file);
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<StreamGoalsConfig>(json);
    }
}