namespace SpekkieClassLibrary.OBS.Events;

public class InputNameChangedEventArgs : EventArgs
{
    public InputNameChangedEventArgs(string oldInputName, string inputName)
    {
        OldInputName = oldInputName;
        InputName = inputName;
    }

    public string OldInputName { get; }
    public string InputName { get; }
}