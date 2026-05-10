using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Tests;

public class ChannelPointsFeatureTests
{
    private readonly Mock<ICustomTwitchHttpClient> _Client = new();
    private readonly Mock<ITwitchAuthTokenProvider> _Tokens = new();
    private readonly Mock<ISpotifyService> _Spotify = new();
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);

    private ChannelPointsFeature CreateFeature() =>
        new(_Client.Object, _Tokens.Object, _Spotify.Object, _Logger.Object);

    private static ChannelPointRedeemed Redemption(string title, string? input = "spotify:track:abc") =>
        new("rid1", "rwid1", title, "uid", "user1", input, DateTimeOffset.UtcNow);

    private void SetupStatusUpdate() =>
        _Tokens.Setup(t => t.ReadIdentityAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new TwitchGeneralFile { ChannelId = "123" });

    // ── Guard clauses ────────────────────────────────────────────────────────

    [Fact]
    public async Task OnRedeemed_EmptyTitle_ReturnsNoTitleError()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption(""), CancellationToken.None);
        Assert.Equal("No reward title found", result);
    }

    [Fact]
    public async Task OnRedeemed_WhitespaceTitle_ReturnsNoTitleError()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("   "), CancellationToken.None);
        Assert.Equal("No reward title found", result);
    }

    // ── Song Request ─────────────────────────────────────────────────────────

    [Fact]
    public async Task OnRedeemed_SongRequest_NoInput_ReturnsInputRequired()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request", null), CancellationToken.None);
        Assert.Equal("User input is required for this reward", result);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyAdds_ReturnsSuccessMessage()
    {
        SetupStatusUpdate();
        _Spotify.Setup(s => s.AddSongToQueueAsync("spotify:track:abc", It.IsAny<CancellationToken>()))
                .ReturnsAsync("My Song by Artist");

        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        Assert.Contains("Successfully added", result);
        Assert.Contains("My Song by Artist", result);
        Assert.Contains("queue", result);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyAdds_TagsUser()
    {
        SetupStatusUpdate();
        _Spotify.Setup(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("My Song by Artist");

        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        Assert.StartsWith("@user1", result);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyAdds_UpdatesStatusFulfilled()
    {
        SetupStatusUpdate();
        _Spotify.Setup(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("My Song by Artist");

        await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        _Client.Verify(c => c.PatchAsync(
            It.IsAny<string>(),
            It.Is<StringContent>(sc => sc.ReadAsStringAsync().Result.Contains("FULFILLED")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyFails_ReturnsFailureMessage()
    {
        SetupStatusUpdate();
        _Spotify.Setup(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Error");

        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        Assert.Contains("Failed to add", result);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyFails_UpdatesStatusCancelled()
    {
        SetupStatusUpdate();
        _Spotify.Setup(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Error");

        await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        _Client.Verify(c => c.PatchAsync(
            It.IsAny<string>(),
            It.Is<StringContent>(sc => sc.ReadAsStringAsync().Result.Contains("CANCELED")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Hydrate ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnRedeemed_Hydrate_ReturnsHydrateMessage()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("Hydrate"), CancellationToken.None);

        Assert.Equal("Hydrate reward redeemed", result);
    }

    // ── Unknown reward ───────────────────────────────────────────────────────

    [Fact]
    public async Task OnRedeemed_UnknownReward_ReturnsIgnored()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("Something Else"), CancellationToken.None);
        Assert.Equal("Ignored", result);
    }

    [Fact]
    public async Task OnRedeemed_UnknownReward_DoesNotUpdateStatus()
    {
        await CreateFeature().OnRedeemedAsync(Redemption("Something Else"), CancellationToken.None);

        _Client.Verify(c => c.PatchAsync(
            It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}