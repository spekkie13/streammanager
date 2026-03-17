namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface ITwitchCommandHandler
{
    Task<string> HandleCreateRedemptionCommand(string commandArgs);
}
