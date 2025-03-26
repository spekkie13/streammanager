using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Timer;

namespace EventTimerService
{
    public class EventTimer
    {
        private readonly TimerFileWriter _timerFileWriter;
        private readonly TimerFileReader _timerFileReader;
        private readonly Logger _logger;
        private TimeSpan _RemainingTime;
        private bool _isRunning;
        private readonly Timer _timer;

        public EventTimer(TimerFileWriter timerFileWriter, TimerFileReader timerFileReader, Logger logger)
        {
            _timerFileWriter = timerFileWriter;
            _timerFileReader = timerFileReader;
            _logger = logger;
            SetupTimer();
            _timerFileWriter.WriteRemainingTime(_RemainingTime);
            _timer = new Timer(CountDownTick, null, Timeout.Infinite, 1000); // Initially paused
            _isRunning = false;
        }

        private void SetupTimer()
        {
            string remainingTime = _timerFileReader.ReadRemainingTime();
            _RemainingTime = TimeSpan.Parse(remainingTime);
        }

        private void CountDownTick(object? state)
        {
            if (!_isRunning || _RemainingTime <= TimeSpan.Zero)
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
            _timerFileWriter.WriteRemainingTime(totalTime);
        }

        public void StartTimer()
        {
            if (_isRunning) return;
            _timer.Change(0, 1000); // Start immediately, tick every second
            _isRunning = true;
        }

        public void StopTimer()
        {
            if (!_isRunning) return;
            _timer.Change(Timeout.Infinite, Timeout.Infinite); // Pause timer
            _isRunning = false;
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
            _logger.LogInfo($"Added {extraTime}, new remaining time: {_RemainingTime}");
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
