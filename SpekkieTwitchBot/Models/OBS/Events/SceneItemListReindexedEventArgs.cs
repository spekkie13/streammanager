using Newtonsoft.Json.Linq;

namespace SpekkieTwitchBot.Models.OBS.Events;

public class SceneItemListReindexedEventArgs : EventArgs
{
    public string SceneName { get; } 

    public List<JObject> SceneItems { get; }

    public SceneItemListReindexedEventArgs(string sceneName, List<JObject> sceneItems)
    {
        SceneName = sceneName;
        SceneItems = sceneItems;
    }
}