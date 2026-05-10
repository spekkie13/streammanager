namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemLockStateChangedEventArgs : EventArgs
{
    public SceneItemLockStateChangedEventArgs(string sceneName, int sceneItemId, bool sceneItemLocked)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
        SceneItemLocked = sceneItemLocked;
    }

    public string SceneName { get; }
    public int SceneItemId { get; }
    public bool SceneItemLocked { get; }
}