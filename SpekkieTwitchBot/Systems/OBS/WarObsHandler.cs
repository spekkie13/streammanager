using SpekkieClassLibrary.Events;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.OBS.Websocket;

namespace SpekkieTwitchBot.Systems.OBS;

public class WarObsHandler(IObsWebSocket obs, IStreamEventBus eventBus, WarStatus warStatus, Logger logger)
{
    public void Register()
    {
        eventBus.Subscribe<WarStateChangedEvent>(HandleWarStateChanged);
    }

    private Task HandleWarStateChanged(WarStateChangedEvent e, CancellationToken ct)
    {
        if (warStatus.Mode != WarDisplayMode.Auto)
            return Task.CompletedTask;

        try
        {
            string sceneName = obs.GetCurrentProgramScene();

            // Best-effort: scenes without these sources are skipped rather than throwing into the event bus.
            ObsSceneItems.TrySetEnabled(obs, sceneName, "Chatbox", !e.IsActive, logger);
            ObsSceneItems.TrySetEnabled(obs, sceneName, "War Stats", e.IsActive, logger);
        }
        catch (Exception ex)
        {
            // OBS not connected / scene lookup failed — the overlay state is still correct, so don't
            // let it take down the war-state handler.
            logger.LogError($"[OBS] War state toggle failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
