namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public interface IWarService
{
    void SetWarStats(bool enable);
    bool GetWarStatus();
    void UpdatePlayerTag(string playerTag);
}
