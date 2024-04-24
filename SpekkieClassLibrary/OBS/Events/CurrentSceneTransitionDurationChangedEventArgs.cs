namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneTransitionDurationChangedEventArgs : EventArgs
{
    public int TransitionDuration { get; }

    public CurrentSceneTransitionDurationChangedEventArgs(int transitionDuration)
    {
        TransitionDuration = transitionDuration;
    }
}