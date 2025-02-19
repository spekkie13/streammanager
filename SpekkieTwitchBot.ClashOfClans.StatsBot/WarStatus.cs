namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class WarStatus
{
    private bool _StatsOn;

    public bool GetStatus()
    {
        return _StatsOn;
    }
    
    public void SetStatus(bool value)
    {
        _StatsOn = value;
    }
}