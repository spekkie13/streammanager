namespace SpekkieClassLibrary.OBS.Events;

public class StudioModeStateChangedEventArgs : EventArgs
{
    public StudioModeStateChangedEventArgs(bool studioModeEnabled)
    {
        StudioModeEnabled = studioModeEnabled;
    }

    public bool StudioModeEnabled { get; }
}