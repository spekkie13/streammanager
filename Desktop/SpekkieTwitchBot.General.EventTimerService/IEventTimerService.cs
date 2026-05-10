namespace EventTimerService;

public interface IEventTimerService
{
    void StartTimer();
    void StopTimer();
    void RestartTimer();
    void AddTime(TimeSpan timeSpan);
    void SetRemainingTime(TimeSpan timeSpan);
    TimeSpan GetRemainingTime();
}
