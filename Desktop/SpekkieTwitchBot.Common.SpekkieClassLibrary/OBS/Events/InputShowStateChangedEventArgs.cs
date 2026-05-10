namespace SpekkieClassLibrary.OBS.Events;

public class InputShowStateChangedEventArgs : EventArgs
{
    public InputShowStateChangedEventArgs(string inputName, bool videoShowing)
    {
        InputName = inputName;
        VideoShowing = videoShowing;
    }

    public string InputName { get; }
    public bool VideoShowing { get; }
}