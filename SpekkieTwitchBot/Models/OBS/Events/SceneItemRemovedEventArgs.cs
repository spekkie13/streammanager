using OBSWebsocketDotNet;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneItemRemovedEventArgs : EventArgs
{
    public string SceneName { get; }
    public string SourceName { get; }
    public int SceneItemId { get; }
    public SceneItemRemovedEventArgs(string sceneName, string sourceName, int sceneItemId)
    {
        SceneName = sceneName;
        SourceName = sourceName;
        SceneItemId = sceneItemId;
    }
}