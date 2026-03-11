using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.Systems.OBS;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ClashCommandHandler
{
    private readonly WarService _WarService;
    private readonly ObsWebSocket _ObsWebSocket;

    public ClashCommandHandler(WarService warService, ObsWebSocket obsWebSocket)
    {
        _WarService = warService;
        _ObsWebSocket = obsWebSocket;
    }

    public string HandleToggleWarStatsCommand()
    {
        _WarService.ToggleWarStats();
        bool status = _WarService.GetWarStatus();

        string sceneName = _ObsWebSocket.GetCurrentProgramScene();
        int chatBoxId = _ObsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "Chatbox", searchOffset: 0);
        _ObsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: chatBoxId, sceneItemEnabled: !status);

        int warStatsId = _ObsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "Browser", searchOffset: 0);
        _ObsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: warStatsId, sceneItemEnabled: status);

        return status ? "War service has been turned on" : "War service has been turned off";
    }

    public string HandleAddPlayerTagCommand(string playerTag)
    {
        _WarService.UpdatePlayerTag(playerTag);
        return $"Updated player tag to: {playerTag}";
    }
}
