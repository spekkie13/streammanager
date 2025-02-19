namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneTransitionDurationChangedEventArgs : EventArgs
{
    public CurrentSceneTransitionDurationChangedEventArgs(int transitionDuration)
    {
        TransitionDuration = transitionDuration;
    }

    public int TransitionDuration { get; }
}