namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public enum WarDisplayMode { Auto, ForceOn, ForceOff }

public class WarStatus
{
    public WarDisplayMode Mode { get; set; } = WarDisplayMode.Auto;
}
