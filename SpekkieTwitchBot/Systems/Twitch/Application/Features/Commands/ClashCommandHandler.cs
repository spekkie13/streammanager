using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.OBS.Websocket;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ClashCommandHandler(IWarService warService, IObsWebSocket obsWebSocket) : IClashCommandHandler
{
    public string HandleSetWarStatsCommand(string argument)
    {
        WarDisplayMode? mode = argument.ToLower() switch
        {
            "on" => WarDisplayMode.ForceOn,
            "off" => WarDisplayMode.ForceOff,
            "auto" => WarDisplayMode.Auto,
            _ => null
        };

        if (mode == null)
            return "Usage: !war on | !war off | !war auto";

        warService.SetWarMode(mode.Value);

        string sceneName = obsWebSocket.GetCurrentProgramScene();
        int chatBoxId = obsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "Chatbox", searchOffset: 0);
        int warStatsId = obsWebSocket.GetSceneItemId(sceneName: sceneName, sourceName: "War Stats", searchOffset: 0);

        bool showWar = mode switch
        {
            WarDisplayMode.ForceOn => true,
            WarDisplayMode.ForceOff => false,
            WarDisplayMode.Auto => warService.IsWarActive,
            _ => false
        };

        obsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: chatBoxId, sceneItemEnabled: !showWar);
        obsWebSocket.SetSceneItemEnabled(sceneName: sceneName, sceneItemId: warStatsId, sceneItemEnabled: showWar);

        return mode switch
        {
            WarDisplayMode.ForceOn => "War stats forced on",
            WarDisplayMode.ForceOff => "War stats forced off",
            WarDisplayMode.Auto => "War stats set to auto mode",
            _ => "War mode updated"
        };
    }

    public async Task<string> HandleAddPlayerTagCommand(string playerTag)
    {
        await warService.UpdatePlayerTag(playerTag);
        return $"Updated player tag to: {playerTag}";
    }
}
