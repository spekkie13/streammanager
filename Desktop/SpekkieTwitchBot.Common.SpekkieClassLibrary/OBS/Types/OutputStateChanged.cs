using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Types;

public class OutputStateChanged
{
    private OutputState? _State;

    public OutputStateChanged(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public OutputStateChanged()
    {
    }

    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }

    [JsonProperty(PropertyName = "outputState")]
    public string? StateStr { get; set; }

    public OutputState State
    {
        get
        {
            if (_State.HasValue)
                return _State.Value;

            switch (StateStr)
            {
                case "ObsWebsocketOutputStarting":
                    _State = OutputState.ObsWebsocketOutputStarting;
                    break;
                case "ObsWebsocketOutputStarted":
                    _State = OutputState.ObsWebsocketOutputStarted;
                    break;
                case "ObsWebsocketOutputStopping":
                    _State = OutputState.ObsWebsocketOutputStopping;
                    break;
                case "ObsWebsocketOutputStopped":
                    _State = OutputState.ObsWebsocketOutputStopped;
                    break;
                case "ObsWebsocketOutputPaused":
                    _State = OutputState.ObsWebsocketOutputPaused;
                    break;
                case "ObsWebsocketOutputResumed":
                    _State = OutputState.ObsWebsocketOutputResumed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return _State.Value;
        }
    }
}