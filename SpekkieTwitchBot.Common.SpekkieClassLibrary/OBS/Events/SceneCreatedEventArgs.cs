namespace SpekkieClassLibrary.OBS.Events;

public class SceneCreatedEventArgs : EventArgs
{
    public SceneCreatedEventArgs(string sceneName, bool isGroup)
    {
        SceneName = sceneName;
        IsGroup = isGroup;
    }

    public string SceneName { get; }
    public bool IsGroup { get; }
}