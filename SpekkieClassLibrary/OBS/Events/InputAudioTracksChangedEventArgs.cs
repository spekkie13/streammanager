using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

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