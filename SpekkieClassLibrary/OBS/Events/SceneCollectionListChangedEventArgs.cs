namespace SpekkieClassLibrary.OBS.Events;

public class SceneCollectionListChangedEventArgs : EventArgs
{
    public List<string> SceneCollections { get; }
    
    public SceneCollectionListChangedEventArgs(List<string> sceneCollections)
    {
        SceneCollections = sceneCollections;
    }
}