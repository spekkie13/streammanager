namespace SpekkieTwitchBot.Models.OBS.Events;

public class CurrentSceneCollectionChangedEventArgs : EventArgs
{
    public string SceneCollectionName { get; }
    public CurrentSceneCollectionChangedEventArgs(string sceneCollectionName)
    {
        SceneCollectionName = sceneCollectionName;
    }
}