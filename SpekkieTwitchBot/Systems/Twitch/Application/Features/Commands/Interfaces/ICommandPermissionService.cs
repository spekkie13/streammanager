using SpekkieClassLibrary.Twitch;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface ICommandPermissionService
{
    bool IsAllowed(string command, UserRole userRole);
}
