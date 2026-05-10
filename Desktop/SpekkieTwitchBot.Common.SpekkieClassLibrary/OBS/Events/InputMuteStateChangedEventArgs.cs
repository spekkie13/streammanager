namespace SpekkieClassLibrary.OBS.Events;

public class InputMuteStateChangedEventArgs : EventArgs
{
    public InputMuteStateChangedEventArgs(string inputName, bool inputMuted)
    {
        InputName = inputName;
        InputMuted = inputMuted;
    }

    public string InputName { get; }
    public bool InputMuted { get; }
}