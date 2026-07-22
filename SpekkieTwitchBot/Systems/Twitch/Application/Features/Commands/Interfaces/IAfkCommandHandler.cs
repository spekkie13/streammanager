namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface IAfkCommandHandler
{
    Task<string> HandleAfkCommand(CancellationToken ct);
    Task<string> HandleBackCommand(CancellationToken ct);
}
