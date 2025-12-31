using System.Text.Json;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public enum PubSubInboundKind { Unknown, Response, Message, Pong, Reconnect }

public sealed record PubSubInboundMessage(
    PubSubInboundKind Kind,
    string? Topic = null,
    string? Nonce = null,
    bool? Success = null,
    string? Error = null,

    // Domain fields (MVP)
    string? UserId = null,
    string? UserName = null,
    SubHappened? Sub = null,
    ChannelPointRedeemed? Redemption = null
);

public sealed class PubSubMessageParser
{
    public PubSubInboundMessage Parse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (type is null) return new(PubSubInboundKind.Unknown);

        switch (type)
        {
            case "PONG":
                return new(PubSubInboundKind.Pong);

            case "RECONNECT":
                return new(PubSubInboundKind.Reconnect);

            case "RESPONSE":
            {
                var nonce = root.GetProperty("nonce").GetString();
                var err = root.GetProperty("error").GetString();
                var ok = string.IsNullOrEmpty(err);
                return new(PubSubInboundKind.Response, Nonce: nonce, Success: ok, Error: err);
            }

            case "MESSAGE":
            {
                // MVP: only expose topic; later parse message.data.message JSON per topic
                var data = root.GetProperty("data");
                var topic = data.GetProperty("topic").GetString();

                // Later: decode data.message (stringified JSON)
                return new(PubSubInboundKind.Message, Topic: topic);
            }

            default:
                return new(PubSubInboundKind.Unknown);
        }
    }
}