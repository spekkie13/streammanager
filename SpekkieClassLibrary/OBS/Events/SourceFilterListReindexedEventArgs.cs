using SpekkieClassLibrary.OBS.Types;

namespace SpekkieClassLibrary.OBS.Events
{
    public class SourceFilterListReindexedEventArgs : EventArgs
    {
        public string SourceName { get; }
        public List<FilterReorderItem> Filters { get; }

        public SourceFilterListReindexedEventArgs(string sourceName, List<FilterReorderItem> filters)
        {
            SourceName = sourceName;
            Filters = filters;
        }
    }
}