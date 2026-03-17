using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface IGeneralCommandHandler
{
    Task<string> HandleCommand(ChatCommandReceived command, CancellationToken ct);
}
