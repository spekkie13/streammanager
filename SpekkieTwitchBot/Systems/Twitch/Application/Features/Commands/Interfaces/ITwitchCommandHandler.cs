namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface ITwitchCommandHandler
{
    Task<string> HandleCreateRedemptionCommand(string commandArgs);
    Task<string> HandleUptimeCommand(CancellationToken ct);
    Task<string> HandleClipCommand(CancellationToken ct);
    Task<string> HandleShoutoutCommand(string username, CancellationToken ct);
}
