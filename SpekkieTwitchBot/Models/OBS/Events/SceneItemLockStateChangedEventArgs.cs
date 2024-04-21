namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneItemLockStateChangedEventArgs : EventArgs
{
    public string SceneName { get; }
    public int SceneItemId { get; }
    public bool SceneItemLocked { get; }
    public SceneItemLockStateChangedEventArgs(string sceneName, int sceneItemId, bool sceneItemLocked)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
        SceneItemLocked = sceneItemLocked;
    }
}