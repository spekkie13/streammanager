namespace SpekkieClassLibrary.OBS.Events;

public class SourceFilterRemovedEventArgs : EventArgs
{
    public SourceFilterRemovedEventArgs(string sourceName, string filterName)
    {
        SourceName = sourceName;
        FilterName = filterName;
    }

    public string SourceName { get; }
    public string FilterName { get; }
}