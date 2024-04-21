namespace SpekkieTwitchBot.Models.OBS.Events;

public class SourceFilterEnableStateChangedEventArgs : EventArgs
{
    public string SourceName { get; }
    public string FilterName { get; }
    public bool FilterEnabled { get; }

    public SourceFilterEnableStateChangedEventArgs(string sourceName, string filterName, bool filterEnabled)
    {
        SourceName = sourceName;
        FilterName = filterName;
        FilterEnabled = filterEnabled;
    }
}