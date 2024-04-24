namespace SpekkieClassLibrary.OBS.Events;

public class InputRemovedEventArgs : EventArgs
{
    public string InputName { get; }

    public InputRemovedEventArgs(string inputName)
    {
        InputName = inputName;
    }
}