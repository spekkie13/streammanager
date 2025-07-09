using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using TwitchLib.PubSub.Models.Responses.Messages.AutomodCaughtMessage;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class AutomodCaughtMessage : AutomodQueueData
{
    [JsonProperty(PropertyName = "content_classification")]
    public ContentClassification? ContentClassification { get; protected set; }

    [JsonProperty(PropertyName = "message")]
    public Message? Message { get; protected set; }

    [JsonProperty(PropertyName = "reason_code")]
    public string? ReasonCode { get; protected set; }

    [JsonProperty(PropertyName = "resolver_id")]
    public string? ResolverId { get; protected set; }

    [JsonProperty(PropertyName = "resolver_login")]
    public string? ResolverLogin { get; protected set; }

    [JsonProperty(PropertyName = "status")]
    public string? Status { get; protected set; }
    
    public static AutomodCaughtMessage Empty =>  new AutomodCaughtMessage();
}