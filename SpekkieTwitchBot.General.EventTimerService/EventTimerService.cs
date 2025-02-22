using Microsoft.Extensions.Hosting;

namespace EventTimerService
{
    public sealed class EventTimerService(EventTimer timer) : IHostedService
    {
        public Task StartAsync(CancellationToken token)
        {
            Console.WriteLine("Timer active, waiting to start.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            StopTimer();
            return Task.CompletedTask;
        }

        public void StartTimer()
        {
            Console.WriteLine("Starting timer...");
            timer.StartTimer();
        }

        public void StopTimer()
        {
            Console.WriteLine("Stopping timer...");
            timer.StopTimer();
        }

        public void RestartTimer()
        {
            Console.WriteLine("Restarting timer...");
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