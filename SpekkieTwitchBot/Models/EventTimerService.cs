using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.Models.Twitch;

namespace SpekkieTwitchBot.Models;

public sealed class EventTimerService : IHostedService
{
    private EventTimer _EventTimer;

    public EventTimerService(EventTimer timer)
    {
        _EventTimer = timer;
    }
    
    public Task StartAsync(CancellationToken token)
    {
        _EventTimer.Timer.Change(0, 1000);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        _EventTimer.Timer.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public void SetRemainingTime(TimeSpan timeSpan)
    {
        _EventTimer.SetRemainingTime(timeSpan);
    }

    public TimeSpan GetRemainingTime()
    {
        return _EventTimer.GetRemainingTime();
    }
}