namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemSelectedEventArgs : EventArgs
{
    public string SceneName { get; }
    public string SceneItemId { get; }

    public SceneItemSelectedEventArgs(string sceneName, string sceneItemId)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
    }
}