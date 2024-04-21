using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class StreamStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public StreamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}