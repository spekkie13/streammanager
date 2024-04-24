using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class ReplayBufferStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public ReplayBufferStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}