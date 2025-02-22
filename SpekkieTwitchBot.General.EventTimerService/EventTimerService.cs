using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventTimerService
{
    public sealed class EventTimerService : IHostedService
    {
        private readonly EventTimer _EventTimer;

        public EventTimerService(EventTimer timer)
        {
            _EventTimer = timer;
        }

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
            _EventTimer.StartTimer();
        }

        public void StopTimer()
        {
            Console.WriteLine("Stopping timer...");
            _EventTimer.StopTimer();
        }

        public void RestartTimer()
        {
            Console.WriteLine("Restarting timer...");
            _EventTimer.RestartTimer();
        }

        public void AddTime(TimeSpan timeSpan)
        {
            _EventTimer.AddTime(timeSpan);
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
}