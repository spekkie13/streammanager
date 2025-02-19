namespace SpekkieClassLibrary.OBS.Events;

public class InputAudioSyncOffsetChangedEventArgs : EventArgs
{
    public InputAudioSyncOffsetChangedEventArgs(string inputName, int inputAudioSyncOffset)
    {
        InputName = inputName;
        InputAudioSyncOffset = inputAudioSyncOffset;
    }

    public string InputName { get; }
    public int InputAudioSyncOffset { get; }
}