namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public interface IWarService
{
    void SetWarStats(bool enable);
    bool GetWarStatus();
    Task UpdatePlayerTag(string playerTag);
}
