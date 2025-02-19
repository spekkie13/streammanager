namespace SpekkieClassLibrary.OBS.Events;

public class SceneTransitionEndedEventArgs : EventArgs
{
    public SceneTransitionEndedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }

    public string TransitionName { get; }
}