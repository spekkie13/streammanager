namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemEnableStateChangedEventArgs(string sceneName, int sceneItemId, bool sceneItemEnabled) : EventArgs
{
    public string SceneName { get; } = sceneName;
    public int SceneItemId { get; } = sceneItemId;
    public bool SceneItemEnabled { get; } = sceneItemEnabled;
}