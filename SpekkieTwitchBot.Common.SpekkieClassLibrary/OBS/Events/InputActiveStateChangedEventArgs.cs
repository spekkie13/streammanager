namespace SpekkieClassLibrary.OBS.Events;

public class InputActiveStateChangedEventArgs : EventArgs
{
    public InputActiveStateChangedEventArgs(string inputName, bool videoActive)
    {
        InputName = inputName;
        VideoActive = videoActive;
    }

    public string InputName { get; }
    public bool VideoActive { get; }
}