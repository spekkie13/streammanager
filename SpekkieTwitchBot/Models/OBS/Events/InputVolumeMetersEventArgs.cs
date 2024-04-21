using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputVolumeMetersEventArgs : EventArgs
{
    public List<JObject> inputs { get; }

    public InputVolumeMetersEventArgs(List<JObject> inputs)
    {
        this.inputs = inputs;
    }
}