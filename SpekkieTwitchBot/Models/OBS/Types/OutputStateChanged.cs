using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class OutputStateChanged
{
    private OutputState? state;

    [JsonProperty(PropertyName = "outputActive")]
    public bool IsActive { get; set; }
    
    [JsonProperty(PropertyName = "outputState")]
    public string StateStr { get; set; }

    public OutputState State
    {
        get
        {
            if (state.HasValue)
                return state.Value;

            switch (StateStr)
            {
                case "OBS_WEBSOCKET_OUTPUT_STARTING":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_STARTING;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STARTED":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_STARTED;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STOPPING":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_STOPPED":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_PAUSED":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED;
                    break;
                case "OBS_WEBSOCKET_OUTPUT_RESUMED":
                    state = OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return state.Value;
        }
    }
    
    public OutputStateChanged(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }
    
    public OutputStateChanged(){ }
}