namespace SpekkieClassLibrary.OBS.Events;

public class MediaInputPlaybackEndedEventArgs : EventArgs
{
    public string InputName { get; }

    public MediaInputPlaybackEndedEventArgs(string inputName)
    {
        InputName = inputName;
    }
}