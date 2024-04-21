namespace SpekkieTwitchBot.Models.OBS.Events;

public class ProgramSceneChangedEventArgs : EventArgs
{
    public string SceneName { get; }

    public ProgramSceneChangedEventArgs(string sceneName)
    {
        SceneName = sceneName;
    }
}