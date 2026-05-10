namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneTransitionChangedEventArgs : EventArgs
{
    public CurrentSceneTransitionChangedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }

    public string TransitionName { get; }
}