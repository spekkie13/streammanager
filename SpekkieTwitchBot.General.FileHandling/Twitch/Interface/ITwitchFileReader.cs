namespace SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

public interface ITwitchFileReader
{
    public string ReadTwitchUserAuthFile();
    public string ReadTwitchGeneralAuthFile();
    public Task<string> ReadMostRecentFollowerFileAsync();
    public Task<string> ReadMostRecentSubFileAsync();
    public Task<string> ReadSubGoalFileAsync();
    public Task<string> ReadFollowerGoalFileAsync();
}