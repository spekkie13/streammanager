namespace SpekkieClassLibrary.OBS.Events;

public class CurrentPreviewSceneChangedEventArgs : EventArgs
{
    public string SceneName { get; }

    public CurrentPreviewSceneChangedEventArgs(string sceneName)
    {
        SceneName = sceneName;
    }
}