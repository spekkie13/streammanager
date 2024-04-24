namespace SpekkieClassLibrary.OBS.Events;

public class SceneTransitionVideoEndedEventArgs : EventArgs
{
    public string TransitionName { get; }
    public SceneTransitionVideoEndedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }
}