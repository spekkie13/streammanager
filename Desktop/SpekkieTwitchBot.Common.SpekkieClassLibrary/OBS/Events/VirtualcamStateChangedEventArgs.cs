using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class VirtualcamStateChangedEventArgs : EventArgs
{
    public VirtualcamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }

    public OutputStateChanged OutputState { get; }
}