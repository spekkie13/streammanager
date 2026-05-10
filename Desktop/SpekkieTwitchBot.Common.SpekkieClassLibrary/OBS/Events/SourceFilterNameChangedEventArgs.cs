namespace SpekkieClassLibrary.OBS.Events;

public class SourceFilterNameChangedEventArgs : EventArgs
{
    public SourceFilterNameChangedEventArgs(string sourceName, string oldFilterName, string filterName)
    {
        SourceName = sourceName;
        OldFilterName = oldFilterName;
        FilterName = filterName;
    }

    public string SourceName { get; }
    public string OldFilterName { get; }
    public string FilterName { get; }
}