using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface ITextCommandHandler
{
    List<TextCommand> GetTextCommands();
    string HandleCommand(ChatCommandReceived command);
    string AddCommand(string action, string command, string response);
}
