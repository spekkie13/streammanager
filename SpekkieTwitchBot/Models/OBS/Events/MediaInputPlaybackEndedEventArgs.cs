using OBSWebsocketDotNet;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class MediaInputPlaybackEndedEventArgs : EventArgs
{
    public string InputName { get; }

    public MediaInputPlaybackEndedEventArgs(string inputName)
    {
        InputName = inputName;
    }
}