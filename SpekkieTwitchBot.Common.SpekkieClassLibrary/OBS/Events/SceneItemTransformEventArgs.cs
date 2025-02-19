using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemTransformEventArgs : EventArgs
{
    public SceneItemTransformEventArgs(string sceneName, string sceneItemId, SceneItemTransformInfo transform)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
        Transform = transform;
    }

    public string SceneName { get; }
    public string SceneItemId { get; }
    public SceneItemTransformInfo Transform { get; }
}