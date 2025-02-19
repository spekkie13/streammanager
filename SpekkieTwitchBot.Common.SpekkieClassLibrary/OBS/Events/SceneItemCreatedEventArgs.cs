namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemCreatedEventArgs : EventArgs
{
    public SceneItemCreatedEventArgs(string sceneName, string sourceName, int sceneItemId, int sceneItemIndex)
    {
        SceneName = sceneName;
        SourceName = sourceName;
        SceneItemId = sceneItemId;
        SceneItemIndex = sceneItemIndex;
    }

    public string SceneName { get; }
    public string SourceName { get; }
    public int SceneItemId { get; }
    public int SceneItemIndex { get; }
}