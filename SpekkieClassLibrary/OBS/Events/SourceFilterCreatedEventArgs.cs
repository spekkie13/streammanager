using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class SourceFilterCreatedEventArgs : EventArgs
{
    public string SourceName { get; }
    public string FilterName{ get; }
    public string FilterKind{ get; }
    public int FilterIndex{ get; }
    public JObject FilterSettings{ get; }
    public JObject DefaultFilterSettings { get; }

    public SourceFilterCreatedEventArgs(string sourceName, string filterName, string filterKind, int filterIndex, JObject filterSettings, JObject defaultFilterSettings)
    {
        SourceName = sourceName;
        FilterName = filterName;
        FilterKind = filterKind;
        FilterIndex = filterIndex;
        FilterSettings = filterSettings;
        DefaultFilterSettings = defaultFilterSettings;
    }
}