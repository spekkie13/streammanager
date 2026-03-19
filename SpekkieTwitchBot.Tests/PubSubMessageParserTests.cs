using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

namespace SpekkieTwitchBot.Tests;

public class PubSubMessageParserTests
{
    [Fact]
    public void Parse_PongMessage_ReturnsPong()
    {
        string raw = """{"type":"PONG"}""";

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Pong, result.Kind);
    }

    [Fact]
    public void Parse_ReconnectMessage_ReturnsReconnect()
    {
        string raw = """{"type":"RECONNECT"}""";

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Reconnect, result.Kind);
    }

    [Fact]
    public void Parse_SuccessfulResponse_ReturnsSuccessTrue()
    {
        string raw = """{"type":"RESPONSE","nonce":"abc123","error":""}""";

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Response, result.Kind);
        Assert.True(result.Success);
        Assert.Equal("abc123", result.Nonce);
        Assert.Equal("", result.Error);
    }

    [Fact]
    public void Parse_ResponseWithError_ReturnsFailure()
    {
        string raw = """{"type":"RESPONSE","nonce":"xyz","error":"ERR_BADAUTH"}""";

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Response, result.Kind);
        Assert.False(result.Success);
        Assert.Equal("ERR_BADAUTH", result.Error);
    }

    [Fact]
    public void Parse_UnknownType_ReturnsUnknown()
    {
        string raw = """{"type":"SOMETHING_NEW"}""";

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Unknown, result.Kind);
    }

    [Fact]
    public void Parse_MessageWithNonRedemptionInnerType_ReturnsUnknown()
    {
        string innerData = """{"type":"update-redemption-status","data":{}}""";
        string raw = $$"""
        {
            "type": "MESSAGE",
            "data": {
                "topic": "channel-points-channel-v1.123456",
                "message": {{System.Text.Json.JsonSerializer.Serialize(innerData)}}
            }
        }
        """;

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Unknown, result.Kind);
    }

    [Fact]
    public void Parse_MessageWithNoInnerType_ReturnsUnknown()
    {
        // Simulates a follow-like payload that has no "type" field in the inner message
        string innerData = """{"display_name":"cooluser","username":"cooluser","user_id":"123"}""";
        string raw = $$"""
        {
            "type": "MESSAGE",
            "data": {
                "topic": "following.123456",
                "message": {{System.Text.Json.JsonSerializer.Serialize(innerData)}}
            }
        }
        """;

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Unknown, result.Kind);
    }

    [Fact]
    public void Parse_MessageWithRedemption_NoUserInput_SetsEmptyUserInput()
    {
        string innerData = """
        {
            "type": "reward-redeemed",
            "data": {
                "redemption": {
                    "id": "rid",
                    "user": {"id": "u1", "login": "user1"},
                    "reward": {"id": "r1", "title": "Hydrate"},
                    "redeemed_at": "2024-01-01T12:00:00Z"
                }
            }
        }
        """;
        string raw = $$"""
        {
            "type": "MESSAGE",
            "data": {
                "topic": "channel-points-channel-v1.123456",
                "message": {{System.Text.Json.JsonSerializer.Serialize(innerData)}}
            }
        }
        """;

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Message, result.Kind);
        Assert.NotNull(result.Redemption);
        Assert.Equal("", result.Redemption.UserInput);
    }

    [Fact]
    public void Parse_MessageWithRedemption_ParsesRedemptionCorrectly()
    {
        string innerData = """
        {
            "type": "reward-redeemed",
            "data": {
                "redemption": {
                    "id": "redemption-id-1",
                    "user": {
                        "id": "user123",
                        "login": "cooluser"
                    },
                    "reward": {
                        "id": "reward-id-1",
                        "title": "Hydrate!"
                    },
                    "redeemed_at": "2024-01-01T12:00:00Z",
                    "user_input": "staying hydrated"
                }
            }
        }
        """;

        string raw = $$"""
        {
            "type": "MESSAGE",
            "data": {
                "topic": "channel-points-channel-v1.123456",
                "message": {{System.Text.Json.JsonSerializer.Serialize(innerData)}}
            }
        }
        """;

        PubSubInboundMessage result = PubSubMessageParser.Parse(raw);

        Assert.Equal(PubSubInboundKind.Message, result.Kind);
        Assert.Equal("channel-points-channel-v1.123456", result.Topic);
        Assert.NotNull(result.Redemption);
        Assert.Equal("reward-id-1", result.Redemption.RewardId);
        Assert.Equal("Hydrate!", result.Redemption.RewardTitle);
        Assert.Equal("user123", result.Redemption.UserId);
        Assert.Equal("cooluser", result.Redemption.UserName);
        Assert.Equal("redemption-id-1", result.Redemption.RedemptionId);
        Assert.Equal("staying hydrated", result.Redemption.UserInput);
    }
}
