using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class SceneListChangedEventArgs : EventArgs
{
    public List<JObject> Scenes { get; }

    public SceneListChangedEventArgs(List<JObject> scenes)
    {
        Scenes = scenes;
    }
}