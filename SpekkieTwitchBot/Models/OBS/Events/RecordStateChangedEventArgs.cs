using SpekkieTwitchBot.Models.OBS.Types;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class RecordStateChangedEventArgs : EventArgs
{
    public RecordStateChanged OutputState { get; }

    public RecordStateChangedEventArgs(RecordStateChanged outputState)
    {
        OutputState = outputState;
    }
}