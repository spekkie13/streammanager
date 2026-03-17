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
    public static PubSubInboundMessage Parse(string raw)
    {
        using JsonDocument doc = JsonDocument.Parse(raw);
        JsonElement root = doc.RootElement;

        string? type = root.TryGetProperty("type", out JsonElement t) ? t.GetString() : null;
        if (type is null) return new PubSubInboundMessage(PubSubInboundKind.Unknown);

        switch (type)
        {
            case "PONG":
                return new PubSubInboundMessage(PubSubInboundKind.Pong);

            case "RECONNECT":
                return new PubSubInboundMessage(PubSubInboundKind.Reconnect);

            case "RESPONSE":
            {
                string? nonce = root.GetProperty("nonce").GetString();
                string? err = root.GetProperty("error").GetString();
                bool ok = string.IsNullOrEmpty(err);
                return new PubSubInboundMessage(PubSubInboundKind.Response, Nonce: nonce, Success: ok, Error: err);
            }

            case "MESSAGE":
            {
                JsonElement data = root.GetProperty("data");
                string? topic = data.GetProperty("topic").GetString();

                JsonElement inner = ParseInnerMessage(data);

                string? innerType = inner.TryGetProperty("type", out JsonElement innerT) ? innerT.GetString() : null;
                if (innerType != "reward-redeemed")
                    return new PubSubInboundMessage(PubSubInboundKind.Unknown);

                JsonElement redemptionElement = inner.GetProperty("data").GetProperty("redemption");
                JsonElement rewardElement = redemptionElement.GetProperty("reward");

                string rewardId = rewardElement.GetProperty("id").GetString() ?? "";
                string rewardTitle = rewardElement.GetProperty("title").GetString() ?? "";
                string userId = redemptionElement.GetProperty("user").GetProperty("id").GetString() ?? "";
                string userName = redemptionElement.GetProperty("user").GetProperty("login").GetString() ?? "";
                string redemptionId = redemptionElement.GetProperty("id").GetString() ?? "";
                DateTimeOffset redemptionTime = DateTimeOffset.Parse(redemptionElement.GetProperty("redeemed_at").GetString() ?? DateTimeOffset.UtcNow.ToString("O"));
                string userInput = "";
                if (redemptionElement.TryGetProperty("user_input", out JsonElement userInputStr))
                    userInput = userInputStr.GetString() ?? "";

                ChannelPointRedeemed redemption = new ChannelPointRedeemed(
                    RewardId: rewardId,
                    RewardTitle: rewardTitle,
                    UserId: userId,
                    UserName: userName,
                    RedemptionId: redemptionId,
                    RedeemedAt: redemptionTime,
                    UserInput: userInput
                );

                return new PubSubInboundMessage(PubSubInboundKind.Message, Topic: topic, Redemption: redemption);
            }

            default:
                return new PubSubInboundMessage(PubSubInboundKind.Unknown);
        }
    }
    
    private static JsonElement ParseInnerMessage(JsonElement outer)
    {
        string? innerJson = outer.GetProperty("message").GetString();
        if (string.IsNullOrWhiteSpace(innerJson))
            throw new InvalidOperationException("data.message was empty");

        using JsonDocument innerDoc = JsonDocument.Parse(innerJson);
        return innerDoc.RootElement.Clone(); // Clone zodat je hem buiten using kan gebruiken
    }
}