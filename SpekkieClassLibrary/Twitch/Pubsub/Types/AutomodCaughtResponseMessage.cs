using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class AutomodCaughtResponseMessage : UserModerationNotificationsData
{
    [JsonProperty(PropertyName = "message_id")]
    public string MessageId { get; protected set; }

    [JsonProperty(PropertyName = "status")]
    public string Status { get; protected set; }
}