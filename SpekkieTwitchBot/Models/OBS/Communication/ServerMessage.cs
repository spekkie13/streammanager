using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieTwitchBot.Models.OBS.Enum;

namespace SpekkieTwitchBot.Models.OBS.Communication
{
    /// <summary>
    /// Message received from the server
    /// </summary>
    internal class ServerMessage
    {
        /// <summary>
        /// Server Message's operation code
        /// </summary>
        [JsonProperty(PropertyName = "op")]
        public MessageTypes OperationCode { set; get; }

        /// <summary>
        /// Server Data
        /// </summary>
        [JsonProperty(PropertyName = "d")]
        public JObject Data { get; set; }
    }
}