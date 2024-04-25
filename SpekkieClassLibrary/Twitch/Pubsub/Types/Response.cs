#nullable disable
using Newtonsoft.Json.Linq;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class Response
{
    public string Error { get; }
    public string Nonce { get; }
    public bool Successful { get; }

    public Response(string json)
    {
        Error = JObject.Parse(json).SelectToken("error")?.ToString();
        Nonce = JObject.Parse(json).SelectToken("nonce")?.ToString();
        if (!string.IsNullOrWhiteSpace(Error))
            return;
        Successful = true;
    }
}