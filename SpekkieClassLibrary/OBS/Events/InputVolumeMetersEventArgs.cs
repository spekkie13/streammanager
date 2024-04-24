using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class InputVolumeMetersEventArgs(List<JObject> inputs) : EventArgs
{
    public List<JObject> Inputs { get; } = inputs;
}