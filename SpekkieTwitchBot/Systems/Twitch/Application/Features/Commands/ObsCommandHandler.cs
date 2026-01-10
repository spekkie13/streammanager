using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Systems.OBS;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ObsCommandHandler
{
    private readonly ObsWebSocket _Socket;
    
    public ObsCommandHandler(ObsWebSocket socket)
    {
        _Socket = socket;
    }
    
    public string HandleSetSceneCommand(string sceneName)
    {
        sceneName = string.Concat(sceneName[0].ToString().ToUpper(), sceneName.AsSpan(1));
        _Socket.SetCurrentProgramScene(sceneName);
        
        return $"Changing scene to {sceneName}";
    }

    public string HandleSetInputVolume(string inputName, string volume)
    {
        return "";
    }

    public string HandleSetInputMute(string inputName)
    {
        inputName = string.Concat(inputName[0].ToString().ToUpper(), inputName.AsSpan(1));
        bool currentMuteStatus = _Socket.GetInputMute(inputName);
        _Socket.SetInputMute(inputName, !currentMuteStatus);
        return currentMuteStatus ? $"{inputName} set to unmuted" : $"{inputName} set to muted";
    }

    public string HandleSetStandardVolumes()
    {
        List<InputBasicInfo> inputCaptures = _Socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _Socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures)
            _Socket.SetInputVolume(input.InputName, ObsStandards.StandardMicVolume, true);

        foreach (InputBasicInfo output in outputCaptures)
            _Socket.SetInputVolume(output.InputName, ObsStandards.StandardMusicVolume, true);
        
        return "Set standard volumes";
    }

    public string HandleVolumeZero()
    {
        List<InputBasicInfo> inputCaptures = _Socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _Socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures) _Socket.SetInputVolume(input.InputName, (float)0.0);

        foreach (InputBasicInfo output in outputCaptures) _Socket.SetInputVolume(output.InputName, (float)0.0);

        return "All inputs & outputs muted";
    }
}