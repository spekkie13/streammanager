using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubMessageBuilder
{
    public static string BuildListen(IEnumerable<string> topics, string userAccessToken)
    {
        // PubSub expects token without "oauth:" prefix
        string token = userAccessToken.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase)
            ? userAccessToken["oauth:".Length..]
            : userAccessToken;

        PubSubPayload payload = new PubSubPayload
        {
            type = "LISTEN",
            nonce = Guid.NewGuid().ToString("N")[..8],
            data = new
            {
                topics = topics.ToArray(),
                auth_token = token
            }
        };

        return JsonSerializer.Serialize(payload);
    }
}