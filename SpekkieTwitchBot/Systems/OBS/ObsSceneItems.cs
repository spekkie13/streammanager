using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.OBS.Websocket;

namespace SpekkieTwitchBot.Systems.OBS;

// Sources like "Chatbox" / "War Stats" only live in some scenes. Asking OBS for an item that isn't in
// the current program scene returns ErrorCode 600 ("No scene items were found"), which used to bubble
// out of the war-state event handler on every boot and every war-state transition. Toggling is
// best-effort: a missing item just means this scene doesn't show that source.
public static class ObsSceneItems
{
    private const int NoSceneItemsFound = 600;

    public static bool TrySetEnabled(IObsWebSocket obs, string sceneName, string sourceName, bool enabled,
        Logger? logger = null)
    {
        try
        {
            int itemId = obs.GetSceneItemId(sceneName, sourceName, 0);
            obs.SetSceneItemEnabled(sceneName, itemId, enabled);
            return true;
        }
        catch (ErrorResponseException ex) when (ex.ErrorCode == NoSceneItemsFound)
        {
            logger?.LogInfo($"[OBS] Scene '{sceneName}' has no item '{sourceName}' — skipping toggle");
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogError($"[OBS] Failed to toggle '{sourceName}' in '{sceneName}': {ex.Message}");
            return false;
        }
    }
}
