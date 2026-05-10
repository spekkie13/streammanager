using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class RecordStateChangedEventArgs : EventArgs
{
    public RecordStateChangedEventArgs(RecordStateChanged outputState)
    {
        OutputState = outputState;
    }

    public RecordStateChanged OutputState { get; }
}