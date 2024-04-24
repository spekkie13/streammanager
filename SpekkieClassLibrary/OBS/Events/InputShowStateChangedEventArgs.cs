namespace SpekkieClassLibrary.OBS.Events;

public class InputShowStateChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public bool VideoShowing { get; }
    public InputShowStateChangedEventArgs(string inputName, bool videoShowing)
    {
        InputName = inputName;
        VideoShowing = videoShowing;
    }
}