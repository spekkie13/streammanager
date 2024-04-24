using SpekkieTwitchBot.Timer;
using SpekkieTwitchBot.Twitch.General;

namespace SpekkieTwitchBot.Twitch.Commands;

public class TimerCommandHandler
{
    private readonly EventTimerService _EventTimerService;
    private readonly IrcClient _IrcClient;

    public TimerCommandHandler(EventTimerService eventTimerService, IrcClient ircClient)
    {
        _EventTimerService = eventTimerService;
        _IrcClient = ircClient;
    }
    
    public void HandlePauseTimerCommand()
    {
        _EventTimerService.StopAsync(default);
        _IrcClient.SendPublicChatMessage($"Pausing timer at: {_EventTimerService.GetRemainingTime()}");
    }

    public void HandleStartTimerCommand()
    {
        _EventTimerService.StartAsync(CancellationToken.None);
        _IrcClient.SendPublicChatMessage($"Resuming timer at: {_EventTimerService.GetRemainingTime()}");
    }
    
    public void HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan initialRemainingTime = _EventTimerService.GetRemainingTime();
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                int duration = Convert.ToInt32(timeToAdd.Split('s')[0]);
                TimeSpan time = initialRemainingTime + TimeSpan.FromSeconds(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} seconds to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                duration = Convert.ToInt32(timeToAdd.Split('m')[0]);
                time = initialRemainingTime + TimeSpan.FromMinutes(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} minutes to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                duration = Convert.ToInt32(timeToAdd.Split('h')[0]);
                time = initialRemainingTime + TimeSpan.FromHours(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} hours to the timer");
                break;
        }
    }

    public void HandleSetTimeOnTimerCommand(string time)
    {
        string[] parts = time.Split(":");
        TimeSpan newTime = new TimeSpan(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
        _IrcClient.SendPublicChatMessage($"Set timer to {parts[0].PadLeft(2, '0')}:{parts[1].PadLeft(2, '0')}:{parts[2].PadLeft(2, '0')}");
        _EventTimerService.SetRemainingTime(newTime);
    }
}