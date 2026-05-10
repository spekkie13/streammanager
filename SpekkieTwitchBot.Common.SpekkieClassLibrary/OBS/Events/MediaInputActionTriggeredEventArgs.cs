namespace SpekkieClassLibrary.OBS.Events;

public class MediaInputActionTriggeredEventArgs : EventArgs
{
    public MediaInputActionTriggeredEventArgs(string inputName, string mediaAction)
    {
        InputName = inputName;
        MediaAction = mediaAction;
    }

    public string InputName { get; }
    public string MediaAction { get; }
}