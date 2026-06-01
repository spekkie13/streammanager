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

    [Fact]
    public void ShouldInclude_FiltersThirdPartyBotByLogin()
    {
        ChatMessageReceived e = new("mid", "uid", "Nightbot", "stay hydrated", null) { Login = "nightbot" };
        Assert.False(ChatOverlayService.ShouldInclude(e, botName: "spekkie"));
    }

    // ── Metadata mapping ───────────────────────────────────────────────────────

    [Fact]
    public void Append_MapsColorLoginAndUserId()
    {
        ChatOverlayService service = CreateService();

        service.Append(new ChatMessageReceived("mid", "uid42", "Viewer", "hi", null)
        {
            Login = "viewer", Color = "#1E90FF"
        });

        var m = service.Snapshot()[0];
        Assert.Equal("#1E90FF", m.Color);
        Assert.Equal("viewer", m.Login);
        Assert.Equal("uid42", m.UserId);
    }

    [Fact]
    public void Append_ParsesBadgesIntoOrderedListAndRoleFlags()
    {
        ChatOverlayService service = CreateService();

        service.Append(new ChatMessageReceived("mid", "uid", "Streamer", "hello", null)
        {
            Badges = "broadcaster/1,subscriber/12"
        });

        var m = service.Snapshot()[0];
        Assert.Equal(new[] { "broadcaster/1", "subscriber/12" }, m.Badges);
        Assert.True(m.IsBroadcaster);
        Assert.True(m.IsSubscriber);
        Assert.False(m.IsMod);
        Assert.False(m.IsVip);
    }

    [Fact]
    public void Append_StripsActionWrapperAndSetsIsAction()
    {
        ChatOverlayService service = CreateService();
        string marker = ((char)1).ToString();

        service.Append(Msg(marker + "ACTION waves" + marker));

        var m = service.Snapshot()[0];
        Assert.True(m.IsAction);
        Assert.Equal("waves", m.Text);
    }

    [Fact]
    public void Append_MapsReplyParent()
    {
        ChatOverlayService service = CreateService();

        service.Append(new ChatMessageReceived("mid", "uid", "Viewer", "yes!", null)
        {
            ReplyParentDisplayName = "OtherUser", ReplyParentBody = "are you there?"
        });

        var m = service.Snapshot()[0];
        Assert.NotNull(m.ReplyParent);
        Assert.Equal("OtherUser", m.ReplyParent!.User);
        Assert.Equal("are you there?", m.ReplyParent.Text);
    }

    [Fact]
    public void Append_NoReplyParent_LeavesNull()
    {
        ChatOverlayService service = CreateService();
        service.Append(Msg("plain message"));
        Assert.Null(service.Snapshot()[0].ReplyParent);
    }

    // ── Emote segmentation ─────────────────────────────────────────────────────

    [Fact]
    public void BuildSegments_NoEmotesTag_ReturnsEmpty()
        => Assert.Empty(ChatOverlayService.BuildSegments("hello world", ""));

    [Fact]
    public void BuildSegments_EmoteInMiddle_SplitsTextEmoteText()
    {
        var segs = ChatOverlayService.BuildSegments("Hello Kappa world", "25:6-10");

        Assert.Equal(3, segs.Count);
        Assert.Equal("text", segs[0].Type);
        Assert.Equal("Hello ", segs[0].Text);
        Assert.Equal("emote", segs[1].Type);
        Assert.Equal("25", segs[1].EmoteId);
        Assert.Equal("Kappa", segs[1].Text);
        Assert.Equal(" world", segs[2].Text);
    }

    [Fact]
    public void BuildSegments_EmoteAtStart_NoLeadingTextSegment()
    {
        var segs = ChatOverlayService.BuildSegments("Kappa rest", "25:0-4");

        Assert.Equal(2, segs.Count);
        Assert.Equal("emote", segs[0].Type);
        Assert.Equal(" rest", segs[1].Text);
    }

    [Fact]
    public void BuildSegments_MultipleEmotesAndRepeats_OrderedByPosition()
    {
        // "Kappa PogChamp Kappa" — Kappa(25) at 0-4 and 15-19, PogChamp(88) at 6-13.
        var segs = ChatOverlayService.BuildSegments("Kappa PogChamp Kappa", "25:0-4,15-19/88:6-13");

        Assert.Equal(5, segs.Count);
        Assert.Equal("25", segs[0].EmoteId);
        Assert.Equal(" ", segs[1].Text);
        Assert.Equal("88", segs[2].EmoteId);
        Assert.Equal(" ", segs[3].Text);
        Assert.Equal("25", segs[4].EmoteId);
    }

    [Fact]
    public void BuildSegments_CountsByCodepointNotUtf16()
    {
        // Leading 😀 is a surrogate pair (2 UTF-16 units, 1 codepoint). Kappa is codepoints 2-6.
        var segs = ChatOverlayService.BuildSegments("\U0001F600 Kappa", "25:2-6");

        Assert.Equal(2, segs.Count);
        Assert.Equal("\U0001F600 ", segs[0].Text);
        Assert.Equal("Kappa", segs[1].Text);
    }

    [Fact]
    public void BuildSegments_OutOfRangePositions_RenderAsPlainText()
    {
        // End index past the string: skipped, so the whole text comes back as one text segment.
        var segs = ChatOverlayService.BuildSegments("hi", "25:0-9");

        Assert.Single(segs);
        Assert.Equal("text", segs[0].Type);
        Assert.Equal("hi", segs[0].Text);
    }

    [Fact]
    public void Append_SetsSegmentsFromEmotes()
    {
        ChatOverlayService service = CreateService();

        service.Append(new ChatMessageReceived("mid", "uid", "Viewer", "Hello Kappa", null)
        {
            Emotes = "25:6-10"
        });

        var m = service.Snapshot()[0];
        Assert.Equal(2, m.Segments.Count);
        Assert.Equal("emote", m.Segments[1].Type);
        Assert.Equal("25", m.Segments[1].EmoteId);
    }
}
