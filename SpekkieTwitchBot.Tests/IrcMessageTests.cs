using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;

namespace SpekkieTwitchBot.Tests;

public class IrcMessageTests
{
    [Fact]
    public void Parse_SimplePrivmsg_ExtractsFields()
    {
        string raw = ":nick!nick@nick.tmi.twitch.tv PRIVMSG #channel :hello world";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("#channel", msg.Channel);
        Assert.Equal("hello world", msg.Message);
        Assert.Equal("nick", msg.Username);
        Assert.Equal("nick!nick@nick.tmi.twitch.tv", msg.Prefix);
        Assert.Empty(msg.Tags);
    }

    [Fact]
    public void Parse_WithTags_ParsesTagsCorrectly()
    {
        string raw = "@badge-info=;color=#FF0000;display-name=TestUser :testuser!testuser@testuser.tmi.twitch.tv PRIVMSG #channel :hi";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("", msg.Tags["badge-info"]);
        Assert.Equal("#FF0000", msg.Tags["color"]);
        Assert.Equal("TestUser", msg.Tags["display-name"]);
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("hi", msg.Message);
    }

    [Fact]
    public void Parse_WithTags_ExtractsUsername()
    {
        string raw = "@color=#FF0000 :nick!nick@nick.tmi.twitch.tv PRIVMSG #streamer :test";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("nick", msg.Username);
    }

    [Fact]
    public void Parse_WithoutPrefix_HandlesGracefully()
    {
        string raw = "PING :tmi.twitch.tv";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("PING", msg.Command);
        Assert.Equal("tmi.twitch.tv", msg.Message);
        Assert.Equal("", msg.Prefix);
        Assert.Equal("", msg.Username);
    }

    [Fact]
    public void Parse_TagWithNoValue_StoresEmptyString()
    {
        string raw = "@emptykey= :nick!nick@nick.tmi.twitch.tv PRIVMSG #ch :msg";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.True(msg.Tags.ContainsKey("emptykey"));
        Assert.Equal("", msg.Tags["emptykey"]);
    }

    [Fact]
    public void Parse_PrefixWithoutBang_UsesFullPrefixAsUsername()
    {
        string raw = ":tmi.twitch.tv 001 botname :Welcome";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("tmi.twitch.tv", msg.Username);
        Assert.Equal("001", msg.Command);
    }

    [Fact]
    public void Parse_NoMessage_ReturnsEmptyMessage()
    {
        string raw = ":nick!nick@nick.tmi.twitch.tv JOIN #channel";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("JOIN", msg.Command);
        Assert.Equal("", msg.Message);
    }

    [Fact]
    public void Parse_MessageWithColons_PreservesFullMessage()
    {
        string raw = ":nick!nick@nick.tmi.twitch.tv PRIVMSG #ch :hello: world: test";

        IrcMessage msg = IrcMessage.Parse(raw);

        Assert.Equal("hello: world: test", msg.Message);
    }
}
