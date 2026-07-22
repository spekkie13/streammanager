namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface ITimerCommandHandler
{
    Task<string> HandlePauseTimerCommand(CancellationToken ct);
    Task<string> HandleStartTimerCommand(CancellationToken ct);
    string HandleAddTimeToTimerCommand(string timeToAdd);
    string HandleSetTimeOnTimerCommand(string time);
    Task<string> HandleMarathonCommand(string args);
}
