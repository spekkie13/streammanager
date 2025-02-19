namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneCollectionChangedEventArgs : EventArgs
{
    public CurrentSceneCollectionChangedEventArgs(string sceneCollectionName)
    {
        SceneCollectionName = sceneCollectionName;
    }

    public string SceneCollectionName { get; }
}