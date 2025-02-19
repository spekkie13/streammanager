using SpekkieTwitchBot.General.FileHandling.Timer;

namespace EventTimerService;

public class EventTimer
{
    private readonly TimerFileWriter _timerFileWriter;
    public readonly Timer Timer;
    private TimeSpan _RemainingTime;

    public EventTimer(TimerFileWriter timerFileWriter)
    {
        _timerFileWriter = timerFileWriter;
        _RemainingTime = new TimeSpan(1, 15, 15);
        Timer = new Timer(CountDownTick, null, 1000, 1000);
    }

    private void CountDownTick(object? state)
    {
        WriteFile();
        _RemainingTime -= TimeSpan.FromSeconds(1);
    }

    private void WriteFile()
    {
        var hours = _RemainingTime.Hours + _RemainingTime.Days * 24;
        var minutes = _RemainingTime.Minutes;
        var seconds = _RemainingTime.Seconds;
        var totalTime = new TimeSpan(hours, minutes, seconds);
        _timerFileWriter.WriteRemainingTime(totalTime);
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