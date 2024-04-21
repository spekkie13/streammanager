using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class ReplayBufferStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public ReplayBufferStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}