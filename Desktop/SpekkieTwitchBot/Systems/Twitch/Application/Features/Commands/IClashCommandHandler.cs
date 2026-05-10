namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface IClashCommandHandler
{
    string HandleSetWarStatsCommand(string argument);
    Task<string> HandleAddPlayerTagCommand(string playerTag);
}
