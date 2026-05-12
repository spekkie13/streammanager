namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface IClashCommandHandler
{
    string HandleSetWarStatsCommand(string argument);
    Task<string> HandleAddPlayerTagCommand(string playerTag);
}
