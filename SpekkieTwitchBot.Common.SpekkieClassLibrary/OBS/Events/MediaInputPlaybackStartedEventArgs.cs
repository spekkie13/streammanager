namespace SpekkieClassLibrary.OBS.Events;

public class MediaInputPlaybackStartedEventArgs : EventArgs
{
    public MediaInputPlaybackStartedEventArgs(string inputName)
    {
        InputName = inputName;
    }

    public string InputName { get; }
}