namespace SpekkieTwitchBot.Models.OBS.Events;

public class SourceFilterRemovedEventArgs : EventArgs
{
    public string SourceName { get; }
    public string FilterName { get; }
    public SourceFilterRemovedEventArgs(string sourceName, string filterName)
    {
        SourceName = sourceName;
        FilterName = filterName;
    }
}