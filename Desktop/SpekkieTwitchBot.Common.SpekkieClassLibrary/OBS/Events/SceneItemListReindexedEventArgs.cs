using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.OBS.Events;

public class SceneItemListReindexedEventArgs : EventArgs
{
    public SceneItemListReindexedEventArgs(string sceneName, List<JObject> sceneItems)
    {
        SceneName = sceneName;
        SceneItems = sceneItems;
    }

    public string SceneName { get; }

    public List<JObject> SceneItems { get; }
}