using EventTimerService;
using SpekkieTwitchBot.General.FileHandling.Timer;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TimerCommandHandler : ITimerCommandHandler
{
    private readonly IEventTimerService _EventTimerService;
    private readonly ITimerFileWriter _TimerFileWriter;

    public TimerCommandHandler(IEventTimerService eventTimerService, ITimerFileWriter timerFileWriter)
    {
        _EventTimerService = eventTimerService;
        _TimerFileWriter = timerFileWriter;
    }
    
    public string HandlePauseTimerCommand()
    {
        _EventTimerService.StopTimer();
        return $"Pausing timer at: {_EventTimerService.GetRemainingTime()}";
    }

    public string HandleStartTimerCommand()
    {
        _EventTimerService.StartTimer();
        return $"Resuming timer at: {_EventTimerService.GetRemainingTime()}";
    }

    public string HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan initialRemainingTime = _EventTimerService.GetRemainingTime();
        string message = "";
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                if (!int.TryParse(timeToAdd.Split('s')[0], out int duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                _EventTimerService.SetRemainingTime(time);
                message = $"added {duration} seconds to timer";
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                if (!int.TryParse(timeToAdd.Split('m')[0], out duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                _EventTimerService.SetRemainingTime(time);
                message = $"added {duration} minutes to the timer";
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                if (!int.TryParse(timeToAdd.Split('h')[0], out duration))
                    return "Invalid format. Usage: !addtime <number>s/m/h (e.g. 30s, 5m, 1h)";
                time = initialRemainingTime + TimeSpan.FromHours(duration);
                _EventTimerService.SetRemainingTime(time);
                message = $"added {duration} hours to the timer";
                break;
            
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
        _EventTimerService.SetRemainingTime(newTime);
        return $"Set timer to {hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}