using SpekkieTwitchBot.General;

namespace SpekkieTwitchBot.Models.Twitch;

public class EventTimer
{
    public readonly Timer Timer;
    private TimeSpan _RemainingTime;

    public EventTimer()
    {
        _RemainingTime = new TimeSpan(1,15,15);
        Timer = new Timer(CountDownTick, null, 1000, 1000);
    }

    private void CountDownTick(object? state)
    {
        WriteFile();
        _RemainingTime -= TimeSpan.FromSeconds(1);
    }
    
    private void WriteFile()
    {
        int hours = _RemainingTime.Hours + _RemainingTime.Days * 24;
        int minutes = _RemainingTime.Minutes;
        int seconds = _RemainingTime.Seconds;
        TimeSpan totalTime = new TimeSpan(hours, minutes, seconds);
        FileHandler.WriteRemainingTime(totalTime);
    }

    public void SetRemainingTime(TimeSpan time)
    {
        _RemainingTime = time;
    }

    public TimeSpan GetRemainingTime()
    {
        return _RemainingTime;
    }
}