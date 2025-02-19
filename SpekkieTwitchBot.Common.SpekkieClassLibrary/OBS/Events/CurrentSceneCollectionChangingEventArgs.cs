namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneCollectionChangingEventArgs : EventArgs
{
    public CurrentSceneCollectionChangingEventArgs(string sceneCollectionName)
    {
        SceneCollectionName = sceneCollectionName;
    }

    public string SceneCollectionName { get; }
}