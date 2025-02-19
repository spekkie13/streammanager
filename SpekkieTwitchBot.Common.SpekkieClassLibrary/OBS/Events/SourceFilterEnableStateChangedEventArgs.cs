namespace SpekkieClassLibrary.OBS.Events;

public class SourceFilterEnableStateChangedEventArgs : EventArgs
{
    public SourceFilterEnableStateChangedEventArgs(string sourceName, string filterName, bool filterEnabled)
    {
        SourceName = sourceName;
        FilterName = filterName;
        FilterEnabled = filterEnabled;
    }

    public string SourceName { get; }
    public string FilterName { get; }
    public bool FilterEnabled { get; }
}