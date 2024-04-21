using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneItemTransformEventArgs : EventArgs
{
    public string SceneName { get; } 
    public string SceneItemId { get; } 
    public SceneItemTransformInfo Transform { get; }
    
    public SceneItemTransformEventArgs(string sceneName, string sceneItemId, SceneItemTransformInfo transform)
    {
        SceneName = sceneName;
        SceneItemId = sceneItemId;
        Transform = transform;
    }
}