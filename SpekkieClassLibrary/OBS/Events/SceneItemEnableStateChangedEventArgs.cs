namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemEnableStateChangedEventArgs : EventArgs
{
    public string SceneName { get; }
    public int SceneItemId { get; }
    public bool SceneItemEnabled { get; }

    public SceneItemEnableStateChangedEventArgs(string sceneName, int sceneItemId, bool sceneItemEnabled)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
        SceneItemEnabled = sceneItemEnabled;
    }
}