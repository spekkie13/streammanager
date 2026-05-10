using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class StreamStateChangedEventArgs : EventArgs
{
    public StreamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }

    public OutputStateChanged OutputState { get; }
}