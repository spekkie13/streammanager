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
    private readonly Mock<ICustomTwitchHttpClient> _client = new();
    private readonly Mock<ITwitchAuthTokenProvider> _tokens = new();
    private readonly Mock<ISpotifyService> _spotify = new();
    private readonly Mock<Logger> _logger = new(MockBehavior.Loose, null!);

    private ChannelPointsFeature CreateFeature() =>
        new(_client.Object, _tokens.Object, _spotify.Object, _logger.Object);

    private static ChannelPointRedeemed Redemption(string title, string? input = "spotify:track:abc") =>
        new("rid1", "rwid1", title, "uid", "user1", input, DateTimeOffset.UtcNow);

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
        _tokens.Setup(t => t.ReadIdentityAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new TwitchGeneralFile { ChannelId = "123" });
        _spotify.Setup(s => s.AddSongToQueueAsync("spotify:track:abc", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Added");

        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        Assert.Contains("successfully added", result);
    }

    [Fact]
    public async Task OnRedeemed_SongRequest_SpotifyFails_ReturnsFailureMessage()
    {
        _tokens.Setup(t => t.ReadIdentityAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new TwitchGeneralFile { ChannelId = "123" });
        _spotify.Setup(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Error");

        string result = await CreateFeature().OnRedeemedAsync(Redemption("Song Request"), CancellationToken.None);

        Assert.Contains("failed to add", result);
    }

    // ── Other rewards ────────────────────────────────────────────────────────

    [Fact]
    public async Task OnRedeemed_Hydrate_ReturnsHydrateMessage()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("Hydrate"), CancellationToken.None);
        Assert.Equal("Hydrate reward redeemed", result);
    }

    [Fact]
    public async Task OnRedeemed_UnknownReward_ReturnsIgnored()
    {
        string result = await CreateFeature().OnRedeemedAsync(Redemption("Something Else"), CancellationToken.None);
        Assert.Equal("Ignored", result);
    }
}
