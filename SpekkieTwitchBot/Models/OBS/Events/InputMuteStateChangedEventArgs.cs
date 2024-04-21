namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputMuteStateChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public bool InputMuted { get; }

    public InputMuteStateChangedEventArgs(string inputName, bool inputMuted)
    {
        InputName = inputName;
        InputMuted = inputMuted;
    }
}