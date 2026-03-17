namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface IClashCommandHandler
{
    string HandleSetWarStatsCommand(string argument);
    string HandleAddPlayerTagCommand(string playerTag);
}
