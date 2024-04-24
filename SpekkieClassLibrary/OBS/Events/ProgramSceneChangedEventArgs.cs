namespace SpekkieClassLibrary.OBS.Events;

public class ProgramSceneChangedEventArgs : EventArgs
{
    public string SceneName { get; }

    public ProgramSceneChangedEventArgs(string sceneName)
    {
        SceneName = sceneName;
    }
}