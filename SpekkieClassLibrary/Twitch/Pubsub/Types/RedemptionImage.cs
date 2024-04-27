using Newtonsoft.Json;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class RedemptionImage
{
    [JsonProperty(PropertyName = "url_1x")]
    public string Url1X { get; protected set; }

    [JsonProperty(PropertyName = "url_2x")]
    public string Url2X { get; protected set; }

    [JsonProperty(PropertyName = "url_4x")]
    public string Url4X { get; protected set; }
}