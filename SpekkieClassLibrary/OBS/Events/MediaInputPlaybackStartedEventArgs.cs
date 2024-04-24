namespace SpekkieClassLibrary.OBS.Events;

public class MediaInputPlaybackStartedEventArgs : EventArgs
{
    public string InputName { get; }

    public MediaInputPlaybackStartedEventArgs(string inputName)
    {
        InputName = inputName;
    }
}