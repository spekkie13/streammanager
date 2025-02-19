namespace SpekkieClassLibrary.OBS.Events;

public class SceneRemovedEventArgs : EventArgs
{
    public SceneRemovedEventArgs(string sceneName, bool isGroup)
    {
        SceneName = sceneName;
        IsGroup = isGroup;
    }

    public string SceneName { get; }
    public bool IsGroup { get; }
}