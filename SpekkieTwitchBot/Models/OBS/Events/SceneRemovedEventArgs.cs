namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneRemovedEventArgs : EventArgs
{
    public string SceneName { get; }
    public bool IsGroup { get; }

    public SceneRemovedEventArgs(string sceneName, bool isGroup)
    {
        SceneName = sceneName;
        IsGroup = isGroup;
    }
}