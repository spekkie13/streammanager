namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputNameChangedEventArgs : EventArgs
{
    public string OldInputName { get; }
    public string InputName { get; }
    public InputNameChangedEventArgs(string oldInputName, string inputName)
    {
        OldInputName = oldInputName;
        InputName = inputName;
    }
}