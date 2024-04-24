using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class InputVolumeChangedEventArgs : EventArgs
{
    public InputVolume Volume { get; }

    public InputVolumeChangedEventArgs(InputVolume volume)
    {
        Volume = volume;
    }
}