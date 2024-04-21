namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputActiveStateChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public bool VideoActive { get; }
    public InputActiveStateChangedEventArgs(string inputName, bool videoActive)
    {
        InputName = inputName;
        VideoActive = videoActive;
    }
}