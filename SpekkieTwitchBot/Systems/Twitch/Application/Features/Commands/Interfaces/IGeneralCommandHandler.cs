using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface IGeneralCommandHandler
{
    Task<string> HandleCommand(ChatCommandReceived command, CancellationToken ct);
}
