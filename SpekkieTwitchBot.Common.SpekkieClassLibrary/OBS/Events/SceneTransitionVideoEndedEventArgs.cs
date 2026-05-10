namespace SpekkieClassLibrary.OBS.Events;

public class SceneTransitionVideoEndedEventArgs : EventArgs
{
    public SceneTransitionVideoEndedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }

    public string TransitionName { get; }
}