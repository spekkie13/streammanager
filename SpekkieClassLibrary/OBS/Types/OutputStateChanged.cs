using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

#nullable disable
namespace SpekkieClassLibrary.OBS.Types;

public class OutputStateChanged
{
    private OutputState? _state;

    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }
    
    [JsonProperty(PropertyName = "outputState")]
    public string StateStr { get; set; }

    public OutputState State
    {
        get
        {
            if (_state.HasValue)
                return _state.Value;

            switch (StateStr)
            {
                case "ObsWebsocketOutputStarting":
                    _state = OutputState.ObsWebsocketOutputStarting;
                    break;
                case "ObsWebsocketOutputStarted":
                    _state = OutputState.ObsWebsocketOutputStarted;
                    break;
                case "ObsWebsocketOutputStopping":
                    _state = OutputState.ObsWebsocketOutputStopping;
                    break;
                case "ObsWebsocketOutputStopped":
                    _state = OutputState.ObsWebsocketOutputStopped;
                    break;
                case "ObsWebsocketOutputPaused":
                    _state = OutputState.ObsWebsocketOutputPaused;
                    break;
                case "ObsWebsocketOutputResumed":
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