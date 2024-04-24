namespace SpekkieClassLibrary.OBS.Events;

public class InputAudioMonitorTypeChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public string MonitorType { get; }
    public InputAudioMonitorTypeChangedEventArgs(string inputName, string monitorType)
    {
        InputName = inputName;
        MonitorType = monitorType;
    }
}