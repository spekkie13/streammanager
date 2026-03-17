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

    public void HandleSetTimerCommand(string time)
    {
        TimeSpan remainingTime = TimeSpan.Parse(time);
        _TimerFileWriter.WriteRemainingTime(remainingTime);
    }

    public string HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan initialRemainingTime = _EventTimerService.GetRemainingTime();
        string message = "";
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                int duration = Convert.ToInt32(timeToAdd.Split('s')[0]);
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                _EventTimerService.SetRemainingTime(time);
                message = $"added {duration} seconds to timer";
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                duration = Convert.ToInt32(timeToAdd.Split('m')[0]);
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                _EventTimerService.SetRemainingTime(time);
                message = $"added {duration} minutes to the timer";
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                duration = Convert.ToInt32(timeToAdd.Split('h')[0]);
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
        TimeSpan newTime =
            new (Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
        _EventTimerService.SetRemainingTime(newTime); 
        return $"Set timer to {parts[0].PadLeft(2, '0')}:{parts[1].PadLeft(2, '0')}:{parts[2].PadLeft(2, '0')}";

    }
}