using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class StreamStateChangedEventArgs : EventArgs
{
    public OutputStateChanged OutputState { get; }

    public StreamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}