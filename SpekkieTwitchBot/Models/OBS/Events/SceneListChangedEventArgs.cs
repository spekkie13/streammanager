using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneListChangedEventArgs : EventArgs
{
    public List<JObject> Scenes { get; }

    public SceneListChangedEventArgs(List<JObject> scenes)
    {
        Scenes = scenes;
    }
}