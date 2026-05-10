namespace SpekkieClassLibrary.OBS.Events;

public class CurrentPreviewSceneChangedEventArgs : EventArgs
{
    public CurrentPreviewSceneChangedEventArgs(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}