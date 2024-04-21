namespace SpekkieTwitchBot.Models.OBS.Events;

public class CurrentSceneCollectionChangingEventArgs : EventArgs
{
    public string SceneCollectionName { get; }
    public CurrentSceneCollectionChangingEventArgs(string sceneCollectionName)
    {
        SceneCollectionName = sceneCollectionName;
    }
}