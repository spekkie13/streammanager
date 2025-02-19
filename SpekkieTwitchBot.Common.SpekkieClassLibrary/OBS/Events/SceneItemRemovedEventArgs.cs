namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemRemovedEventArgs : EventArgs
{
    public SceneItemRemovedEventArgs(string sceneName, string sourceName, int sceneItemId)
    {
        SceneName = sceneName;
        SourceName = sourceName;
        SceneItemId = sceneItemId;
    }

    public string SceneName { get; }
    public string SourceName { get; }
    public int SceneItemId { get; }
}