using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class InputVolumeChangedEventArgs : EventArgs
{
    public InputVolumeChangedEventArgs(InputVolume volume)
    {
        Volume = volume;
    }

    public InputVolume Volume { get; }
}