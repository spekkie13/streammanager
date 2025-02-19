using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class InputCreatedEventArgs : EventArgs
{
    public InputCreatedEventArgs(string inputName, string inputKind, string unversionedInputKind, JObject inputSettings,
        JObject defaultInputSettings)
    {
        InputName = inputName;
        InputKind = inputKind;
        UnversionedInputKind = unversionedInputKind;
        InputSettings = inputSettings;
        DefaultInputSettings = defaultInputSettings;
    }

    public string InputName { get; }
    public string InputKind { get; }
    public string UnversionedInputKind { get; }
    public JObject InputSettings { get; }
    public JObject DefaultInputSettings { get; }
}