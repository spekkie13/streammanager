using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Types;

public class OutputStateChanged
{
    private OutputState? _state;

    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }
    
    [JsonProperty(PropertyName = "outputState")]
    public string? StateStr { get; set; }

    public OutputState State
    {
        get
        {
            if (_state.HasValue)
                return _state.Value;

            switch (StateStr)
            {
                case "OBS_WEBSOCKET_OUTPUT_STARTING":
                    _state = OutputState.ObsWebsocketOutputStarting;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STARTED":
                    _state = OutputState.ObsWebsocketOutputStarted;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STOPPING":
                    _state = OutputState.ObsWebsocketOutputStopping;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STOPPED":
                    _state = OutputState.ObsWebsocketOutputStopped;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_PAUSED":
                    _state = OutputState.ObsWebsocketOutputPaused;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_RESUMED":
                    _state = OutputState.ObsWebsocketOutputResumed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return _state.Value;
        }
    }
    
    public OutputStateChanged(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }
    
    public OutputStateChanged(){ }
}