namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface ITimerCommandHandler
{
    string HandlePauseTimerCommand();
    string HandleStartTimerCommand();
    string HandleAddTimeToTimerCommand(string timeToAdd);
    string HandleSetTimeOnTimerCommand(string time);
}
