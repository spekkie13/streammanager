using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.OBS.Websocket;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class ClashCommandHandler(IWarService warService, IObsWebSocket obsWebSocket, Logger? logger = null)
    : IClashCommandHandler
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

        bool showWar = mode switch
        {
            WarDisplayMode.ForceOn => true,
            WarDisplayMode.ForceOff => false,
            WarDisplayMode.Auto => warService.IsWarActive,
            _ => false
        };

        // Best-effort: the current scene may not contain these sources (OBS ErrorCode 600).
        ObsSceneItems.TrySetEnabled(obsWebSocket, sceneName, "Chatbox", !showWar, logger);
        ObsSceneItems.TrySetEnabled(obsWebSocket, sceneName, "War Stats", showWar, logger);

        return mode switch
        {
            WarDisplayMode.ForceOn => "War stats forced on",
            WarDisplayMode.ForceOff => "War stats forced off",
            WarDisplayMode.Auto => "War stats set to auto mode",
            _ => "War mode updated"
        };
    }
}
