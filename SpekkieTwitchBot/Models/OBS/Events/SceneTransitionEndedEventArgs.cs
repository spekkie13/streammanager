namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneTransitionEndedEventArgs : EventArgs
{
    public string TransitionName { get; }

    public SceneTransitionEndedEventArgs(string transitionName)
    {
        TransitionName = transitionName;
    }
}