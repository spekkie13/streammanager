namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneCreatedEventArgs : EventArgs
{
    public string SceneName { get; }
    public bool IsGroup { get; }
    public SceneCreatedEventArgs(string sceneName, bool isGroup)
    {
        SceneName = sceneName;
        IsGroup = isGroup;
    }
}