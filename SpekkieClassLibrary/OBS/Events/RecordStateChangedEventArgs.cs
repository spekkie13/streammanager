using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events;

public class RecordStateChangedEventArgs : EventArgs
{
    public RecordStateChanged OutputState { get; }

    public RecordStateChangedEventArgs(RecordStateChanged outputState)
    {
        OutputState = outputState;
    }
}