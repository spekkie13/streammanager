using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;

namespace EventTimerService
{
    public sealed class EventTimerService(EventTimer timer, Logger logger) : IHostedService
    {
        public Task StartAsync(CancellationToken token)
        {
            logger.LogInfo("Timer active, waiting to start.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            StopTimer();
            return Task.CompletedTask;
        }

        public void StartTimer()
        {
            logger.LogInfo("Starting timer...");
            timer.StartTimer();
        }

        public void StopTimer()
        {
            logger.LogInfo("Stopping timer...");
            timer.StopTimer();
        }

        public void RestartTimer()
        {
            logger.LogInfo("Restarting timer...");
            timer.RestartTimer();
        }

        public void AddTime(TimeSpan timeSpan)
        {
            timer.AddTime(timeSpan);
        }

        public void SetRemainingTime(TimeSpan timeSpan)
        {
            timer.SetRemainingTime(timeSpan);
        }

        public TimeSpan GetRemainingTime()
        {
            return timer.GetRemainingTime();
        }
    }
}