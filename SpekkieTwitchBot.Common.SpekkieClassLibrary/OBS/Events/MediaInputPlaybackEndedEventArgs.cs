namespace SpekkieClassLibrary.OBS.Events;

public class MediaInputPlaybackEndedEventArgs : EventArgs
{
    public MediaInputPlaybackEndedEventArgs(string inputName)
    {
        InputName = inputName;
    }

    public string InputName { get; }
}