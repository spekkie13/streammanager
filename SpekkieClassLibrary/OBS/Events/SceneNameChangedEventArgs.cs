namespace SpekkieClassLibrary.OBS.Events;

public class SceneNameChangedEventArgs : EventArgs
{
    public string OldSceneName { get; }
    public string SceneName { get; }

    public SceneNameChangedEventArgs(string oldSceneName, string sceneName)
    {
        OldSceneName = oldSceneName;
        SceneName = sceneName;
    }
}