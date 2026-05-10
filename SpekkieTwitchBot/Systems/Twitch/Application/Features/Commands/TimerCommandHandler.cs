using EventTimerService;
using SpekkieTwitchBot.General.FileHandling.Timer;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TimerCommandHandler : ITimerCommandHandler
{
    private readonly IEventTimerService _eventTimerService;
    private readonly ITimerFileWriter _timerFileWriter;

    public TimerCommandHandler(IEventTimerService eventTimerService, ITimerFileWriter timerFileWriter)
    {
        _eventTimerService = eventTimerService;
        _timerFileWriter = timerFileWriter;
    }
    
    public string HandlePauseTimerCommand()
    {
        _eventTimerService.StopTimer();
        return $"Pausing timer at: {_eventTimerService.GetRemainingTime()}";
    }

    public string HandleStartTimerCommand()
    {
        _eventTimerService.StartTimer();
        return $"Resuming timer at: {_eventTimerService.GetRemainingTime()}";
    }

    public string HandleAddTimeToTimerCommand(string timeToAdd)
    {
        if (string.IsNullOrWhiteSpace(timeToAdd))
            return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";

        TimeSpan initialRemainingTime = _eventTimerService.GetRemainingTime();
        string message = "";
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                if (!int.TryParse(timeToAdd.Split('s')[0], out int duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                _eventTimerService.SetRemainingTime(time);
                message = $"added {duration} seconds to timer";
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                if (!int.TryParse(timeToAdd.Split('m')[0], out duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                _eventTimerService.SetRemainingTime(time);
                message = $"added {duration} minutes to the timer";
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                if (!int.TryParse(timeToAdd.Split('h')[0], out duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                time = initialRemainingTime + TimeSpan.FromHours(duration);
                _eventTimerService.SetRemainingTime(time);
                message = $"added {duration} hours to the timer";
                break;

            default:
                return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
        }
        return message;
    }

    public string HandleSetTimeOnTimerCommand(string time)
    {
        string[] parts = time.Split(":");
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int hours)
            || !int.TryParse(parts[1], out int minutes)
            || !int.TryParse(parts[2], out int seconds))
            return "Invalid format. Usage: !settime HH:MM:SS (e.g. 01:30:00)";

        TimeSpan newTime = new(hours, minutes, seconds);
        _eventTimerService.SetRemainingTime(newTime);
        return $"Set timer to {hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}