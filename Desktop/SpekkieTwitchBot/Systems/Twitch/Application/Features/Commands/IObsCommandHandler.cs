namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface IObsCommandHandler
{
    string HandleSetSceneCommand(string sceneName);
    string HandleSetInputMute(string inputName);
    string HandleSetStandardVolumes();
    string HandleVolumeZero();
}
