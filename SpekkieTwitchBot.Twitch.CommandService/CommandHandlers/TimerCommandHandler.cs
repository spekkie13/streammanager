using SpekkieTwitchBot.General.FileHandling.Timer;

namespace CommandService.CommandHandlers;

public class TimerCommandHandler(EventTimerService.EventTimerService eventTimerService, IrcClient ircClient, TimerFileWriter timerFileWriter)
{
    public void HandlePauseTimerCommand()
    {
        eventTimerService.StopTimer();
        ircClient.SendPublicChatMessage($"Pausing timer at: {eventTimerService.GetRemainingTime()}");
    }

    public void HandleStartTimerCommand()
    {
        eventTimerService.StartTimer();
        ircClient.SendPublicChatMessage($"Resuming timer at: {eventTimerService.GetRemainingTime()}");
    }

    public void HandleSetTimerCommand(string time)
    {
        TimeSpan remainingTime = TimeSpan.Parse(time);
        timerFileWriter.WriteRemainingTime(remainingTime);
    }

    public void HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan initialRemainingTime = eventTimerService.GetRemainingTime();
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                int duration = Convert.ToInt32(timeToAdd.Split('s')[0]);
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                eventTimerService.SetRemainingTime(time);
                ircClient.SendPublicChatMessage($"added {duration} seconds to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                duration = Convert.ToInt32(timeToAdd.Split('m')[0]);
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                eventTimerService.SetRemainingTime(time);
                ircClient.SendPublicChatMessage($"added {duration} minutes to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                duration = Convert.ToInt32(timeToAdd.Split('h')[0]);
                time = initialRemainingTime + TimeSpan.FromHours(duration);
                eventTimerService.SetRemainingTime(time);
                ircClient.SendPublicChatMessage($"added {duration} hours to the timer");
                break;
        }
    }

    public void HandleSetTimeOnTimerCommand(string time)
    {
        string[] parts = time.Split(":");
        TimeSpan newTime =
            new TimeSpan(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
        ircClient.SendPublicChatMessage(
            $"Set timer to {parts[0].PadLeft(2, '0')}:{parts[1].PadLeft(2, '0')}:{parts[2].PadLeft(2, '0')}");
        eventTimerService.SetRemainingTime(newTime);
    }
}