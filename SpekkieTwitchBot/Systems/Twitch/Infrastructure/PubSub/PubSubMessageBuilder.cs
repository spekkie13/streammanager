using System.Text.Json;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub.Models;

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
            Type = "LISTEN",
            Nonce = Guid.NewGuid().ToString("N")[..8],
            Data = new
            {
                topics = topics.ToArray(),
                auth_token = token
            }
        };

        return JsonSerializer.Serialize(payload);
    }
}