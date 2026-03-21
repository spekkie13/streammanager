namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public interface IWarService
{
    void SetWarMode(WarDisplayMode mode);
    WarDisplayMode GetWarMode();
    bool IsWarActive { get; }
    Task UpdatePlayerTag(string playerTag);
}
