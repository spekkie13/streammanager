namespace SpekkieClassLibrary.OBS.Events;

public class SceneCollectionListChangedEventArgs : EventArgs
{
    public SceneCollectionListChangedEventArgs(List<string> sceneCollections)
    {
        SceneCollections = sceneCollections;
    }

    public List<string> SceneCollections { get; }
}