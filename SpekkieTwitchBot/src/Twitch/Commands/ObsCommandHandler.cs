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
}