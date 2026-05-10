using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Systems.OBS;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ObsCommandHandler : IObsCommandHandler
{
    private readonly IObsWebSocket _socket;

    public ObsCommandHandler(IObsWebSocket socket)
    {
        _socket = socket;
    }
    
    public string HandleSetSceneCommand(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "No scene name provided";
        sceneName = string.Concat(sceneName[0].ToString().ToUpper(), sceneName.AsSpan(1));
        _socket.SetCurrentProgramScene(sceneName);

        return $"Changing scene to {sceneName}";
    }

    public string HandleSetInputMute(string inputName)
    {
        inputName = string.Concat(inputName[0].ToString().ToUpper(), inputName.AsSpan(1));
        bool currentMuteStatus = _socket.GetInputMute(inputName);
        _socket.SetInputMute(inputName, !currentMuteStatus);
        return currentMuteStatus ? $"{inputName} set to unmuted" : $"{inputName} set to muted";
    }

    public string HandleSetStandardVolumes()
    {
        List<InputBasicInfo> inputCaptures = _socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures)
            _socket.SetInputVolume(input.InputName, ObsStandards.StandardMicVolume, true);

        foreach (InputBasicInfo output in outputCaptures)
            _socket.SetInputVolume(output.InputName, ObsStandards.StandardMusicVolume, true);
        
        return "Set standard volumes";
    }

    public string HandleVolumeZero()
    {
        List<InputBasicInfo> inputCaptures = _socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures) _socket.SetInputVolume(input.InputName, (float)0.0);

        foreach (InputBasicInfo output in outputCaptures) _socket.SetInputVolume(output.InputName, (float)0.0);

        return "All inputs & outputs muted";
    }
}