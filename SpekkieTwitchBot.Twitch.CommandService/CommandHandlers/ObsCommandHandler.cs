using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.OBS.OBSServiceNew;

namespace CommandService.CommandHandlers;

public class ObsCommandHandler(IrcClient ircClient, ObsWebSocket socket)
{
    public void HandleSetSceneCommand(string sceneName)
    {
        sceneName = string.Concat(sceneName[0].ToString().ToUpper(), sceneName.AsSpan(1));
        socket.SetCurrentProgramScene(sceneName);
        ircClient.SendPublicChatMessage($"Changing to scene {sceneName}");
    }

    public void HandleSetInputVolume(string inputName, string volume)
    {
    }

    public void HandleSetInputMute(string inputName)
    {
        inputName = string.Concat(inputName[0].ToString().ToUpper(), inputName.AsSpan(1));
        bool currentMuteStatus = socket.GetInputMute(inputName);
        socket.SetInputMute(inputName, !currentMuteStatus);
        string status = currentMuteStatus ? $"{inputName} set to unmuted" : $"{inputName} set to muted";

        ircClient.SendPublicChatMessage(status);
    }

    public void HandleSetStandardVolumes()
    {
        List<InputBasicInfo> inputCaptures = socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo? input in inputCaptures)
            socket.SetInputVolume(input.InputName, ObsStandards.StandardMicVolume, true);

        foreach (InputBasicInfo? output in outputCaptures)
            socket.SetInputVolume(output.InputName, ObsStandards.StandardMusicVolume, true);
    }

    public void HandleVolumeZero()
    {
        List<InputBasicInfo> inputCaptures = socket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = socket.GetInputList("wasapi_output_capture");

        foreach (InputBasicInfo? input in inputCaptures) socket.SetInputVolume(input.InputName, (float)0.0);

        foreach (InputBasicInfo? output in outputCaptures) socket.SetInputVolume(output.InputName, (float)0.0);
    }
}