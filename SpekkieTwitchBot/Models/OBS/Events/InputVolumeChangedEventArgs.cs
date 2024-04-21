using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputVolumeChangedEventArgs : EventArgs
{
    public InputVolume Volume { get; }

    public InputVolumeChangedEventArgs(InputVolume volume)
    {
        Volume = volume;
    }
}