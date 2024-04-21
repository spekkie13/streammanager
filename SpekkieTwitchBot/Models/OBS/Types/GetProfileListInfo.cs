using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types
{
    /// <summary>
    /// Response from <see cref="OBSWebsocketDotNet.OBSWebsocket.GetProfileList"/>
    /// </summary>
    public class GetProfileListInfo
    {
        /// <summary>
        /// Name of the currently active profile
        /// </summary>
        [JsonProperty(PropertyName = "currentProfileName")]
        public string CurrentProfileName { set; get; }

        /// <summary>
        /// List of all profiles
        /// </summary>
        [JsonProperty(PropertyName = "profiles")]
        public List<string> Profiles { set; get; }
    }
}