using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class InputAudioTracksChangedEventArgs : EventArgs
{
    public InputAudioTracksChangedEventArgs(string inputName, JObject inputAudioTracks)
    {
        InputName = inputName;
        InputAudioTracks = inputAudioTracks;
    }

    public string InputName { get; }
    public JObject InputAudioTracks { get; }
}