namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

public interface IObsCommandHandler
{
    string HandleSetSceneCommand(string sceneName);
    string HandleSetInputMute(string inputName);
    string HandleSetStandardVolumes();
    string HandleVolumeZero();
}
