using SpekkieTwitchBot.General.FileHandling.Timer;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TimerCommandHandler(EventTimerService.EventTimerService eventTimerService, TimerFileWriter timerFileWriter)
{
    public string HandlePauseTimerCommand()
    {
        eventTimerService.StopTimer();
        return $"Pausing timer at: {eventTimerService.GetRemainingTime()}";
    }

    public string HandleStartTimerCommand()
    {
        eventTimerService.StartTimer();
        return $"Resuming timer at: {eventTimerService.GetRemainingTime()}";
    }

    public void HandleSetTimerCommand(string time)
    {
        TimeSpan remainingTime = TimeSpan.Parse(time);
        timerFileWriter.WriteRemainingTime(remainingTime);
    }

    public string HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan initialRemainingTime = eventTimerService.GetRemainingTime();
        string message = "";
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                int duration = Convert.ToInt32(timeToAdd.Split('s')[0]);
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                eventTimerService.SetRemainingTime(time);
                message = $"added {duration} seconds to timer";
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                duration = Convert.ToInt32(timeToAdd.Split('m')[0]);
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                eventTimerService.SetRemainingTime(time);
                message = $"added {duration} minutes to the timer";
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                duration = Convert.ToInt32(timeToAdd.Split('h')[0]);
                time = initialRemainingTime + TimeSpan.FromHours(duration);
                eventTimerService.SetRemainingTime(time);
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
        eventTimerService.SetRemainingTime(newTime); 
        return $"Set timer to {parts[0].PadLeft(2, '0')}:{parts[1].PadLeft(2, '0')}:{parts[2].PadLeft(2, '0')}";

    }
}