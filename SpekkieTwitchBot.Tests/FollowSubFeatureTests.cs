using Moq;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Tests;

public class FollowSubFeatureTests
{
    private readonly Mock<ITwitchChat> _chat = new();
    private readonly Mock<ITwitchChannelInfoClient> _api = new();
    private readonly Mock<ITwitchFileWriter> _files = new();

    private FollowSubFeature CreateFeature() => new(_chat.Object, _api.Object, _files.Object);

    private static SubHappened Sub(SubKind kind, string recipient = "viewer1", string? gifter = null,
        string tier = "1000", int? months = null) =>
        new(kind, "uid1", recipient, null, gifter, tier, false, months, 0, null, DateTimeOffset.UtcNow);

    // ── FormatLatestSub (via WriteMostRecentSubscriberAsync capture) ─────────

    [Fact]
    public async Task HandleSub_NewSub_WritesCorrectFormat()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New, tier: "1000"), CancellationToken.None);

        Assert.Equal("viewer1 subscribed (Tier 1)", written);
    }

    [Fact]
    public async Task HandleSub_Resub_IncludesMonthsAndTier()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Resub, months: 12, tier: "2000"), CancellationToken.None);

        Assert.Equal("viewer1 resubbed (12 months, Tier 2)", written);
    }

    [Fact]
    public async Task HandleSub_Gift_IncludesGifterName()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Gift, gifter: "gifter99", tier: "3000"), CancellationToken.None);

        Assert.Equal("gifter99 gifted a sub to viewer1 (Tier 3)", written);
    }

    [Fact]
    public async Task HandleSub_GiftNullGifter_FallsBackToSomeone()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Gift, gifter: null), CancellationToken.None);

        Assert.Contains("Someone", written);
    }

    [Fact]
    public async Task HandleSub_PrimeTier_MapsToHumanReadable()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New, tier: "prime"), CancellationToken.None);

        Assert.Contains("Prime", written);
    }

    // ── FormatChatThanks (via Chat.SendAsync capture) ───────────────────────

    [Fact]
    public async Task HandleSub_NewSub_SendsWelcomeMessage()
    {
        _api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        string? sent = null;
        _chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent = m)
             .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        Assert.Contains("viewer1", sent);
        Assert.Contains("subscribing", sent);
    }

    [Fact]
    public async Task HandleSub_EmptyRecipient_DoesNothing()
    {
        SubHappened e = new(SubKind.New, "", "", null, null, "1000", false, null, 0, null, DateTimeOffset.UtcNow);

        await CreateFeature().HandleSubAsync(e, CancellationToken.None);

        _files.Verify(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _chat.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── HandleFollowAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleFollow_ValidUser_SendsFollowMessage()
    {
        _api.Setup(a => a.GetFollowerCount(It.IsAny<CancellationToken>())).ReturnsAsync(500);
        _files.Setup(f => f.WriteMostRecentFollowerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        string? sent = null;
        _chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent = m)
             .Returns(Task.CompletedTask);

        await CreateFeature().HandleFollowAsync(new FollowHappened("uid", "follower42", DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Contains("follower42", sent);
    }

    [Fact]
    public async Task HandleFollow_EmptyUsername_DoesNothing()
    {
        await CreateFeature().HandleFollowAsync(new FollowHappened("uid", "", DateTimeOffset.UtcNow), CancellationToken.None);

        _chat.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
