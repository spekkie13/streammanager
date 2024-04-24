namespace SpekkieClassLibrary.OBS.Events;

public class CurrentSceneTransitionChangedEventArgs : EventArgs
{
    public string TransitionName { get; }
    public CurrentSceneTransitionChangedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }
}