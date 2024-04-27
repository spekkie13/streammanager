using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.OBS;
using SpekkieTwitchBot.Twitch.General;

namespace SpekkieTwitchBot.Twitch.Commands;

public class ObsCommandHandler
{
    private readonly IrcClient _IrcClient;
    private readonly CustomObsWebsocket _ObsWebsocket;
    
    public ObsCommandHandler(
        IrcClient ircClient,
        CustomObsWebsocket socket
    )
    {
        _IrcClient = ircClient;
        _ObsWebsocket = socket;
    }

    public void HandleSetSceneCommand(string sceneName)
    {
        sceneName = string.Concat(sceneName[0].ToString().ToUpper(), sceneName.AsSpan(1));
        _ObsWebsocket.SetCurrentProgramScene(sceneName);
        _IrcClient.SendPublicChatMessage($"Changing to scene {sceneName}");
    }

    public void HandleSetInputVolume(string inputName, string volume)
    {
        
    }

    public void HandleSetInputMute(string inputName)
    {
        inputName = string.Concat(inputName[0].ToString().ToUpper(), inputName.AsSpan(1));
        bool currentMuteStatus = _ObsWebsocket.GetInputMute(inputName);
        _ObsWebsocket.SetInputMute(inputName, !currentMuteStatus);
        string status = currentMuteStatus ? $"{inputName} set to unmuted" : $"{inputName} set to muted";
        
        _IrcClient.SendPublicChatMessage(status);
    }
    
    public void HandleSetStandardVolumes()
    {
        //Set input captures to 0.0 db standard
        //Set output captures to standard music volume of -25.3db standard
        List<InputBasicInfo> inputCaptures = _ObsWebsocket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _ObsWebsocket.GetInputList("wasapi_output_capture");

        foreach (var input in inputCaptures)
        {
            _ObsWebsocket.SetInputVolume(input.InputName, ObsStandards.StandardMicVolume, true);
        }

        foreach (var output in outputCaptures)
        {
            _ObsWebsocket.SetInputVolume(output.InputName, ObsStandards.StandardMusicVolume, true);
        }
    }    
    
    public void HandleVolumeZero()
    {
        List<InputBasicInfo> inputCaptures = _ObsWebsocket.GetInputList("wasapi_input_capture");
        List<InputBasicInfo> outputCaptures = _ObsWebsocket.GetInputList("wasapi_output_capture");

        foreach (var input in inputCaptures)
        {
            _ObsWebsocket.SetInputVolume(input.InputName, (float) 0.0);
        }

        foreach (var output in outputCaptures)
        {
            _ObsWebsocket.SetInputVolume(output.InputName, (float) 0.0);
        }
    }
}