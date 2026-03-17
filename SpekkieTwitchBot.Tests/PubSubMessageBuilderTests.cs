using System.Text.Json;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

namespace SpekkieTwitchBot.Tests;

public class PubSubMessageBuilderTests
{
    [Fact]
    public void BuildListen_OauthPrefixedToken_StripsPrefix()
    {
        string json = PubSubMessageBuilder.BuildListen(["topic1"], "oauth:mytoken123");

        using JsonDocument doc = JsonDocument.Parse(json);
        string? token = doc.RootElement.GetProperty("data").GetProperty("auth_token").GetString();
        Assert.Equal("mytoken123", token);
    }

    [Fact]
    public void BuildListen_OauthPrefixUppercase_StripsPrefix()
    {
        string json = PubSubMessageBuilder.BuildListen(["topic1"], "OAuth:mytoken123");

        using JsonDocument doc = JsonDocument.Parse(json);
        string? token = doc.RootElement.GetProperty("data").GetProperty("auth_token").GetString();
        Assert.Equal("mytoken123", token);
    }

    [Fact]
    public void BuildListen_TokenWithoutPrefix_IsUnchanged()
    {
        string json = PubSubMessageBuilder.BuildListen(["topic1"], "plaintoken");

        using JsonDocument doc = JsonDocument.Parse(json);
        string? token = doc.RootElement.GetProperty("data").GetProperty("auth_token").GetString();
        Assert.Equal("plaintoken", token);
    }

    [Fact]
    public void BuildListen_TypeIsAlwaysListen()
    {
        string json = PubSubMessageBuilder.BuildListen(["t"], "tok");

        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal("LISTEN", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void BuildListen_NonceIsEightCharacters()
    {
        string json = PubSubMessageBuilder.BuildListen(["t"], "tok");

        using JsonDocument doc = JsonDocument.Parse(json);
        string? nonce = doc.RootElement.GetProperty("nonce").GetString();
        Assert.Equal(8, nonce?.Length);
    }

    [Fact]
    public void BuildListen_MultipleTopics_AllIncluded()
    {
        string[] topics = ["channel-points.123", "bits.456", "subs.789"];
        string json = PubSubMessageBuilder.BuildListen(topics, "tok");

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement arr = doc.RootElement.GetProperty("data").GetProperty("topics");
        string[] actual = arr.EnumerateArray().Select(e => e.GetString()!).ToArray();

        Assert.Equal(topics, actual);
    }

    [Fact]
    public void BuildListen_ProducesValidJson()
    {
        string json = PubSubMessageBuilder.BuildListen(["t"], "tok");

        // Should not throw
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }
}
