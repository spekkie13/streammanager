using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class VirtualcamStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public VirtualcamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}