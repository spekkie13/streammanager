namespace SpekkieClassLibrary.OBS.Events;

public class InputRemovedEventArgs : EventArgs
{
    public InputRemovedEventArgs(string inputName)
    {
        InputName = inputName;
    }

    public string InputName { get; }
}