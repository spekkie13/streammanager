using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.OBS.OBSServiceNew;

namespace CommandService.CommandHandlers;

public class ClashCommandHandler
{
    private readonly WarService _WarService;
    private readonly IrcClient _IrcClient;
    private readonly ObsWebSocket _ObsWebSocket;
    
    public ClashCommandHandler(WarService warService, IrcClient ircClient, ObsWebSocket obsWebSocket)
    {
        _IrcClient = ircClient;
        _ObsWebSocket = obsWebSocket;
        _WarService = warService;
    }
    
    public void HandleToggleWarStatsCommand()
    {
        _WarService.ToggleWarStats();
        bool status = _WarService.GetWarStatus();

        string sceneName = _ObsWebSocket.GetCurrentProgramScene();
        int chatBoxId = _ObsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "Chatbox", searchOffset: 0);
        _ObsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: chatBoxId, sceneItemEnabled: !status);

        int warStatsId = _ObsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "War Stats", searchOffset: 0);
        _ObsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: warStatsId, sceneItemEnabled: status);
        
        string message = status ? "War service has been turned on" : "War service has been turned off";
        _IrcClient.SendPublicChatMessage(message);
    }

    public void HandleAddPlayerTagCommand(string playerTag)
    {
        _WarService.UpdatePlayerTag(playerTag);
        _IrcClient.SendPublicChatMessage($"Updated player tag to: {playerTag}");
    }
}