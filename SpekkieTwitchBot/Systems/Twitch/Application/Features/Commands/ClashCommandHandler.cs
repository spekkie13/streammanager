using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.Systems.OBS;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ClashCommandHandler(WarService warService, ObsWebSocket obsWebSocket)
{
    public string HandleSetWarStatsCommand(string argument)
    {
        if (argument.ToLower() is not ("on" or "off"))
            return "Usage: !war on | !war off";

        bool enable = argument.Equals("on", StringComparison.CurrentCultureIgnoreCase);
        warService.SetWarStats(enable);

        string sceneName = obsWebSocket.GetCurrentProgramScene();
        int chatBoxId = obsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "Chatbox", searchOffset: 0);
        obsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: chatBoxId, sceneItemEnabled: !enable);

        int warStatsId = obsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "War Stats", searchOffset: 0);
        obsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: warStatsId, sceneItemEnabled: enable);

        return enable ? "War service has been turned on" : "War service has been turned off";
    }

    public string HandleAddPlayerTagCommand(string playerTag)
    {
        warService.UpdatePlayerTag(playerTag);
        return $"Updated player tag to: {playerTag}";
    }
}
