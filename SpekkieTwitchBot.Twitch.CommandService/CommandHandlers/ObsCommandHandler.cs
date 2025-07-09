using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.OBS.OBSServiceNew;

namespace CommandService.CommandHandlers;

public class ObsCommandHandler(ObsWebSocket socket)
{
    public string HandleSetSceneCommand(string sceneName)
    {
        sceneName = string.Concat(sceneName[0].ToString().ToUpper(), sceneName.AsSpan(1));
        socket.SetCurrentProgramScene(sceneName);
        
        return $"Changing scene to {sceneName}";
    }

    public string HandleSetInputVolume(string inputName, string volume)
    {
        return "";
    }

    public string HandleSetInputMute(string inputName)
    {
        inputName = string.Concat(inputName[0].ToString().ToUpper(), inputName.AsSpan(1));
        bool currentMuteStatus = socket.GetInputMute(inputName);
        socket.SetInputMute(inputName, !currentMuteStatus);
        return currentMuteStatus ? $"{inputName} set to unmuted" : $"{inputName} set to muted";
    }

    public string HandleSetStandardVolumes()
    {
        List<InputBasicInfo> inputCaptures = socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures)
            socket.SetInputVolume(input.InputName, ObsStandards.StandardMicVolume, true);

        foreach (InputBasicInfo output in outputCaptures)
            socket.SetInputVolume(output.InputName, ObsStandards.StandardMusicVolume, true);
        
        return "Set standard volumes";
    }

    public string HandleVolumeZero()
    {
        List<InputBasicInfo> inputCaptures = socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo input in inputCaptures) socket.SetInputVolume(input.InputName, (float)0.0);

        foreach (InputBasicInfo output in outputCaptures) socket.SetInputVolume(output.InputName, (float)0.0);

        return "All inputs & outputs muted";
    }
}