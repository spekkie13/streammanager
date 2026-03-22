using Moq;
using SpekkieClassLibrary.Twitch;
using SpotifyAuthService;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Tests;

public class FollowSubFeatureTests
{
    private readonly Mock<ITwitchChat> _Chat = new();
    private readonly Mock<ITwitchChannelInfoClient> _Api = new();
    private readonly Mock<ITwitchFileWriter> _Files = new();
    private readonly Mock<ITwitchFileReader> _FileReader = new();
    private readonly Mock<ISpotifyService> _Spotify = new();

    private FollowSubFeature CreateFeature() =>
        new(_Chat.Object, _Api.Object, _Files.Object, _FileReader.Object, _Spotify.Object);

    private static SubHappened Sub(SubKind kind, string recipient = "viewer1", string? gifter = null,
        string tier = "1000", int? months = null) =>
        new(kind, "uid1", recipient, null, gifter, tier, false, months, 0, null, DateTimeOffset.UtcNow);

    // ── FormatLatestSub (via WriteMostRecentSubscriberAsync capture) ─────────

    [Fact]
    public async Task HandleSub_NewSub_WritesCorrectFormat()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New, tier: "1000"), CancellationToken.None);

        Assert.Equal("viewer1 subscribed (Tier 1)", written);
    }

    [Fact]
    public async Task HandleSub_Resub_IncludesMonthsAndTier()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Resub, months: 12, tier: "2000"), CancellationToken.None);

        Assert.Equal("viewer1 resubbed (12 months, Tier 2)", written);
    }

    [Fact]
    public async Task HandleSub_Gift_IncludesGifterName()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Gift, gifter: "gifter99", tier: "3000"), CancellationToken.None);

        Assert.Equal("gifter99 gifted a sub to viewer1 (Tier 3)", written);
    }

    [Fact]
    public async Task HandleSub_GiftNullGifter_FallsBackToSomeone()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.Gift, gifter: null), CancellationToken.None);

        Assert.Contains("Someone", written);
    }

    [Fact]
    public async Task HandleSub_PrimeTier_MapsToHumanReadable()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        string? written = null;
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, CancellationToken>((s, _) => written = s)
              .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New, tier: "prime"), CancellationToken.None);

        Assert.Contains("Prime", written);
    }

    // ── FormatChatThanks (via Chat.SendAsync capture) ───────────────────────

    [Fact]
    public async Task HandleSub_NewSub_SendsWelcomeMessage()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _Files.Setup(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        string? sent = null;
        _Chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

        _Files.Verify(f => f.WriteMostRecentSubscriberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _Chat.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── HandleFollowAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleFollow_ValidUser_SendsFollowMessage()
    {
        _Api.Setup(a => a.GetFollowerCount(It.IsAny<CancellationToken>())).ReturnsAsync(500);
        _Files.Setup(f => f.WriteMostRecentFollowerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        string? sent = null;
        _Chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent = m)
             .Returns(Task.CompletedTask);

        await CreateFeature().HandleFollowAsync(new FollowHappened("uid", "follower42", DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Contains("follower42", sent);
    }

    [Fact]
    public async Task HandleFollow_EmptyUsername_DoesNothing()
    {
        await CreateFeature().HandleFollowAsync(new FollowHappened("uid", "", DateTimeOffset.UtcNow), CancellationToken.None);

        _Chat.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Sub-goal write path ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleSub_WithGoalsConfig_WritesSubGoalHtml()
    {
        var config = new StreamGoalsConfig(1000, new SubGoalConfig(50, 10, "reward", "beloning", DateOnly.FromDateTime(DateTime.Today.AddDays(30))));
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync(config);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        _Files.Verify(f => f.WriteSubGoalHtml(It.IsAny<StreamGoalsConfig>()), Times.Once);
        _Files.Verify(f => f.WriteGoalsConfig(It.IsAny<StreamGoalsConfig>()), Times.Once);
    }

    [Fact]
    public async Task HandleSub_NullGoalsConfig_DoesNotWriteSubGoalHtml()
    {
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync((StreamGoalsConfig?)null);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        _Files.Verify(f => f.WriteSubGoalHtml(It.IsAny<StreamGoalsConfig>()), Times.Never);
    }

    // ── Music pause on sub ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleSub_PausesSpotify()
    {
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _Spotify.Setup(s => s.PausePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        _Spotify.Verify(s => s.PausePlayerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
