using Moq;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling;
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
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);

    private FollowSubFeature CreateFeature() =>
        new(_Chat.Object, _Api.Object, _Files.Object, _FileReader.Object, _Spotify.Object, _Logger.Object);

    private static SubHappened Sub(SubKind kind, string recipient = "viewer1", string? gifter = null,
        string tier = "1000", int? months = null) =>
        new(kind, "uid1", recipient, null, gifter, tier, false, months, 0, null, DateTimeOffset.UtcNow);

    // Builds a goals config with the given starting count and ordered (goal, rewardEn, rewardNl) tiers.
    private static StreamGoalsConfig Goals(int current, params (int Goal, string En, string Nl)[] tiers) =>
        new(1000, new SubGoalConfig(
            current,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            tiers.Select(t => new SubGoalTier(t.Goal, t.En, t.Nl)).ToList()));

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
        StreamGoalsConfig config = Goals(10, (50, "reward", "beloning"));
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

    // ── Sub-goal reached announcement ────────────────────────────────────────

    [Fact]
    public async Task HandleSub_CrossesGoal_AnnouncesRewardOnce()
    {
        // Goal 1 from a zero count → this first sub crosses the threshold.
        StreamGoalsConfig config = Goals(0, (1, "Extra stream hour", "Extra stream uur"));
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync(config);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        List<string> sent = [];
        _Chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent.Add(m))
             .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        Assert.Contains(sent, m => m.Contains("Sub goal of 1 reached") && m.Contains("Extra stream hour"));
    }

    [Fact]
    public async Task HandleSub_AcrossMultipleSubs_AnnouncesGoalExactlyOnce()
    {
        // Goal 2 from a zero count: only the 2nd sub crosses it; the 3rd must stay silent.
        StreamGoalsConfig config = Goals(0, (2, "Extra stream hour", "Extra stream uur"));
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync(config);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        List<string> sent = [];
        _Chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent.Add(m))
             .Returns(Task.CompletedTask);

        FollowSubFeature feature = CreateFeature();
        await feature.HandleSubAsync(Sub(SubKind.New), CancellationToken.None);
        await feature.HandleSubAsync(Sub(SubKind.New), CancellationToken.None);
        await feature.HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        Assert.Single(sent, m => m.Contains("reached"));
    }

    [Fact]
    public async Task HandleSub_CrossingTier_TeasesNextGoalAndReward()
    {
        // Tiers 1 → 5. The first sub clears tier 1 and should advertise the next milestone (5).
        StreamGoalsConfig config = Goals(0,
            (1, "Extra stream hour", "Extra stream uur"),
            (5, "Subathon day", "Subathon dag"));
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync(config);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        string? sent = null;
        _Chat.Setup(c => c.SendAsync(It.Is<string>(m => m.Contains("reached")), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent = m)
             .Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(SubKind.New), CancellationToken.None);

        Assert.NotNull(sent);
        Assert.Contains("Extra stream hour", sent);
        Assert.Contains("Next goal: 5", sent);
        Assert.Contains("Subathon day", sent);
    }

    [Fact]
    public async Task HandleSub_CommunityGiftClearingMultipleTiers_AnnouncesHighestReached()
    {
        // A community gift of 6 from zero clears tiers 1 and 5 at once → announce the highest (5).
        StreamGoalsConfig config = Goals(0,
            (1, "Reward A", "Beloning A"),
            (5, "Reward B", "Beloning B"));
        _FileReader.Setup(r => r.ReadGoalsConfigAsync()).ReturnsAsync(config);
        _Api.Setup(a => a.GetSubscriberCount(It.IsAny<CancellationToken>())).ReturnsAsync(100);

        string? sent = null;
        _Chat.Setup(c => c.SendAsync(It.Is<string>(m => m.Contains("reached")), It.IsAny<CancellationToken>()))
             .Callback<string, CancellationToken>((m, _) => sent = m)
             .Returns(Task.CompletedTask);

        SubHappened gift = new(SubKind.CommunityGift, "uid1", "viewer1", "gid", "gifter9",
            "1000", false, null, 6, null, DateTimeOffset.UtcNow);
        await CreateFeature().HandleSubAsync(gift, CancellationToken.None);

        Assert.NotNull(sent);
        Assert.Contains("Sub goal of 5 reached", sent);
        Assert.Contains("Reward B", sent);
        Assert.DoesNotContain("Next goal", sent); // no tier beyond 5
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
