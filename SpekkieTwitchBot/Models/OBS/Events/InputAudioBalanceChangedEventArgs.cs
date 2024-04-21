using OBSWebsocketDotNet;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputAudioBalanceChangedEventArgs : EventArgs 
{   
    public string InputName { get; }
    public double InputAudioBalance { get; }
    public InputAudioBalanceChangedEventArgs(string inputName, double inputAudioBalance)
    {
        InputName = inputName;
        InputAudioBalance = inputAudioBalance;
    }
}