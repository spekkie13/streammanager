using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class VendorEventArgs : EventArgs
{
    public string VendorName { get; }
    public string EventType { get; }
    public JObject eventData { get; }
    public VendorEventArgs(string vendorName, string eventType, JObject eventData)
    {
        VendorName = vendorName;
        EventType = eventType;
        this.eventData = eventData;
    }
}