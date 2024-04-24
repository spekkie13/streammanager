using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class VendorEventArgs(string vendorName, string eventType, JObject eventData) : EventArgs
{
    public string VendorName { get; } = vendorName;
    public string EventType { get; } = eventType;
    public JObject EventData { get; } = eventData;
}