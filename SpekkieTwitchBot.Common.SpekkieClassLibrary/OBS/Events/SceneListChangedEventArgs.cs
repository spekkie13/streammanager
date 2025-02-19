using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class SceneListChangedEventArgs : EventArgs
{
    public SceneListChangedEventArgs(List<JObject> scenes)
    {
        Scenes = scenes;
    }

    public List<JObject> Scenes { get; }
}