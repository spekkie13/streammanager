using System.Text.Json;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubMessageBuilder
{
    public string BuildListen(IEnumerable<string> topics, string userAccessToken)
    {
        // PubSub expects token without "oauth:" prefix
        string token = userAccessToken.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase)
            ? userAccessToken["oauth:".Length..]
            : userAccessToken;

        var payload = new
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