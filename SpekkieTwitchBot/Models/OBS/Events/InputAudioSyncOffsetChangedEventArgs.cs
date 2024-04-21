namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputAudioSyncOffsetChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public int InputAudioSyncOffset { get; }
    public InputAudioSyncOffsetChangedEventArgs(string inputName, int inputAudioSyncOffset)
    {
        InputName = inputName;
        InputAudioSyncOffset = inputAudioSyncOffset;
    }
}