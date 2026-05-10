namespace SpekkieClassLibrary.OBS.Events;

public class ProgramSceneChangedEventArgs : EventArgs
{
    public ProgramSceneChangedEventArgs(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}