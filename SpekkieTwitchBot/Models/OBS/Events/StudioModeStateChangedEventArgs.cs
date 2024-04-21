namespace SpekkieTwitchBot.Models.OBS.Events;

public class StudioModeStateChangedEventArgs : EventArgs
{
    public bool StudioModeEnabled { get; }

    public StudioModeStateChangedEventArgs(bool studioModeEnabled)
    {
        StudioModeEnabled = studioModeEnabled;
    }
}