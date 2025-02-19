using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class ReplayBufferStateChangedEventArgs : EventArgs
{
    public ReplayBufferStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }

    public OutputStateChanged OutputState { get; }
}