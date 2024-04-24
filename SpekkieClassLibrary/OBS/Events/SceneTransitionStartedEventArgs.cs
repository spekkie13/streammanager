namespace SpekkieClassLibrary.OBS.Events;

public class SceneTransitionStartedEventArgs : EventArgs
{
    public string TransitionName { get; }

    public SceneTransitionStartedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }
}