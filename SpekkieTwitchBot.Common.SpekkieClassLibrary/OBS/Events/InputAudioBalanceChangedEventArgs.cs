namespace SpekkieClassLibrary.OBS.Events;

public class InputAudioBalanceChangedEventArgs : EventArgs
{
    public InputAudioBalanceChangedEventArgs(string inputName, double inputAudioBalance)
    {
        InputName = inputName;
        InputAudioBalance = inputAudioBalance;
    }

    public string InputName { get; }
    public double InputAudioBalance { get; }
}