using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class InputAudioTracksChangedEventArgs : EventArgs
{
    public string InputName { get; }
    public JObject InputAudioTracks {get;}
    public InputAudioTracksChangedEventArgs(string inputName, JObject inputAudioTracks)
    {
        InputName = inputName;
        InputAudioTracks = inputAudioTracks;
    }
}