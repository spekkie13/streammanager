namespace SpekkieClassLibrary.OBS.Events;

public class SceneTransitionStartedEventArgs : EventArgs
{
    public SceneTransitionStartedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }

    public string TransitionName { get; }
}