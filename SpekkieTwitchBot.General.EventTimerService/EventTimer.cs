using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Timer;

namespace EventTimerService
{
    public class EventTimer
    {
        private readonly TimerFileWriter _TimerFileWriter;
        private readonly TimerFileReader _TimerFileReader;
        private readonly Logger _Logger;
        private TimeSpan _RemainingTime;
        private bool _IsRunning;
        private readonly Timer _Timer;

        public EventTimer(TimerFileWriter timerFileWriter, TimerFileReader timerFileReader, Logger logger)
        {
            _TimerFileWriter = timerFileWriter;
            _TimerFileReader = timerFileReader;
            _Logger = logger;
            SetupTimer();
            _TimerFileWriter.WriteRemainingTime(_RemainingTime);
            _Timer = new Timer(CountDownTick, null, Timeout.Infinite, 1000); // Initially paused
            _IsRunning = false;
        }

        private void SetupTimer()
        {
            string remainingTime = _TimerFileReader.ReadRemainingTime();
            _RemainingTime = TimeSpan.Parse(remainingTime);
        }

        private void CountDownTick(object? state)
        {
            if (!_IsRunning || _RemainingTime <= TimeSpan.Zero)
            {
                StopTimer(); // Stop if time runs out
                return;
            }

            _RemainingTime -= TimeSpan.FromSeconds(1);
            WriteFile();
        }

        private void WriteFile()
        {
            TimeSpan totalTime = new TimeSpan(_RemainingTime.Days * 24 + _RemainingTime.Hours, _RemainingTime.Minutes, _RemainingTime.Seconds);
            _TimerFileWriter.WriteRemainingTime(totalTime);
        }

        public void StartTimer()
        {
            if (_IsRunning) return;
            _Timer.Change(0, 1000); // Start immediately, tick every second
            _IsRunning = true;
        }

        public void StopTimer()
        {
            if (!_IsRunning) return;
            _Timer.Change(Timeout.Infinite, Timeout.Infinite); // Pause timer
            _IsRunning = false;
        }

        public void RestartTimer()
        {
            StopTimer(); // Ensure it stops first
            _RemainingTime = new TimeSpan(6, 0, 0); // Reset to 6 hours
            StartTimer();
        }

        public void AddTime(TimeSpan extraTime)
        {
            _RemainingTime += extraTime;
            _Logger.LogInfo($"Added {extraTime}, new remaining time: {_RemainingTime}");
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
}
