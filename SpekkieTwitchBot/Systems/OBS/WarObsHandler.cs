using SpekkieClassLibrary.Events;

namespace SpekkieTwitchBot.Systems.OBS;

public class WarObsHandler(IObsWebSocket obs, IStreamEventBus eventBus)
{
    public void Register()
    {
        eventBus.Subscribe<WarStateChangedEvent>(HandleWarStateChanged);
    }

    private Task HandleWarStateChanged(WarStateChangedEvent e, CancellationToken ct)
    {
        string sceneName = obs.GetCurrentProgramScene();

        int chatBoxId = obs.GetSceneItemId(sceneName, "Chatbox", 0);
        obs.SetSceneItemEnabled(sceneName, chatBoxId, !e.IsActive);

        int warStatsId = obs.GetSceneItemId(sceneName, "War Stats", 0);
        obs.SetSceneItemEnabled(sceneName, warStatsId, e.IsActive);

        return Task.CompletedTask;
    }
}
