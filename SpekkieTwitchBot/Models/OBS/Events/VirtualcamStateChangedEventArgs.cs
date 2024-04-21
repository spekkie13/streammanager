using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class VirtualcamStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public VirtualcamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}