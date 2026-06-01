using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Tests;

public class ChatOverlayServiceTests
{
    private static ChatMessageReceived Msg(string text, string user = "viewer") =>
        new("mid", "uid", user, text, null);

    private static ChatOverlayService CreateService() =>
        new(new Mock<ITwitchAuthTokenProvider>().Object, new Mock<Logger>(MockBehavior.Loose, null!).Object);

    // ── ShouldInclude filtering ────────────────────────────────────────────────

    [Fact]
    public void ShouldInclude_NormalMessage_True()
        => Assert.True(ChatOverlayService.ShouldInclude(Msg("hello chat"), botName: "spekkie"));

    [Fact]
    public void ShouldInclude_Command_False()
        => Assert.False(ChatOverlayService.ShouldInclude(Msg("!song"), botName: "spekkie"));

    [Fact]
    public void ShouldInclude_EmptyOrWhitespace_False()
    {
        Assert.False(ChatOverlayService.ShouldInclude(Msg(""), botName: "spekkie"));
        Assert.False(ChatOverlayService.ShouldInclude(Msg("   "), botName: "spekkie"));
    }

    [Fact]
    public void ShouldInclude_BotsOwnMessage_False()
        => Assert.False(ChatOverlayService.ShouldInclude(Msg("hi viewer!", user: "Spekkie"), botName: "spekkie"));

    [Fact]
    public void ShouldInclude_KnownThirdPartyBot_False()
        => Assert.False(ChatOverlayService.ShouldInclude(Msg("Stay hydrated!", user: "Nightbot"), botName: "spekkie"));

    [Fact]
    public void ShouldInclude_EmptyBotName_DoesNotFilterSelf()
        => Assert.True(ChatOverlayService.ShouldInclude(Msg("hello", user: "anyone"), botName: ""));

    // ── Buffer behaviour ───────────────────────────────────────────────────────

    [Fact]
    public void Append_KeepsAtMost25MostRecent_OldestFirst()
    {
        ChatOverlayService service = CreateService();

        for (int i = 1; i <= 30; i++)
            service.Append(Msg($"message {i}"));

        IReadOnlyList<SpekkieClassLibrary.Overlay.ChatOverlayMessage> snapshot = service.Snapshot();

        Assert.Equal(25, snapshot.Count);
        Assert.Equal("message 6", snapshot[0].Text);   // oldest retained
        Assert.Equal("message 30", snapshot[^1].Text);  // newest last
    }

    [Fact]
    public void Append_FilteredMessages_NotBuffered()
    {
        ChatOverlayService service = CreateService();

        service.Append(Msg("!command"));
        service.Append(Msg("real message"));
        service.Append(Msg("bot noise", user: "StreamElements"));

        IReadOnlyList<SpekkieClassLibrary.Overlay.ChatOverlayMessage> snapshot = service.Snapshot();

        Assert.Single(snapshot);
        Assert.Equal("real message", snapshot[0].Text);
    }
}
