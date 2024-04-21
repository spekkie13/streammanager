namespace SpekkieTwitchBot.Models.OBS.Events;

public class MediaInputActionTriggeredEventArgs : EventArgs
{
    public string InputName { get; }
    public string MediaAction { get; }

    public MediaInputActionTriggeredEventArgs(string inputName, string mediaAction)
    {
        InputName = inputName;
        MediaAction = mediaAction;
    }
}