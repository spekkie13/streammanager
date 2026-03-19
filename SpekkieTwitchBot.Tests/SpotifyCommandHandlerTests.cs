using Moq;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Tests;

public class SpotifyCommandHandlerTests
{
    private readonly Mock<ISpotifyService> _Spotify = new();
    private readonly Mock<SpotifyFileWriter> _FileWriter = new(MockBehavior.Loose, null!);
    private readonly Mock<ISpotifySearchService> _Search = new();
    private readonly Mock<ITwitchChannelInfoClient> _ChannelInfo = new();

    private SpotifyCommandHandler CreateHandler() =>
        new(_Spotify.Object, _FileWriter.Object, _Search.Object, _ChannelInfo.Object);

    // ── HandleAddSongToQueueCommand ──────────────────────────────────────────

    [Fact]
    public async Task AddSong_TitleArtistFormat_SearchesAndQueues()
    {
        Track track = new() { Uri = "spotify:track:abc123" };
        _Search.Setup(s => s.GetSongsByName("Song", "Artist")).ReturnsAsync(track);
        _Spotify.Setup(s => s.AddSongToQueueAsync("spotify:track:abc123", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Success");

        string result = await CreateHandler().HandleAddSongToQueueCommand("Song|Artist");

        Assert.Equal("Added song to the queue...", result);
    }

    [Fact]
    public async Task AddSong_TitleArtistFormat_SearchFails_ReturnsValidLinkMessage()
    {
        _Search.Setup(s => s.GetSongsByName("Song", "Artist")).ReturnsAsync((Track?)null);

        string result = await CreateHandler().HandleAddSongToQueueCommand("Song|Artist");

        Assert.Equal("Please provide a valid spotify link", result);
    }

    [Fact]
    public async Task AddSong_SpotifyUrl_PassesDirectlyToService()
    {
        string url = "https://open.spotify.com/track/abc123";
        _Spotify.Setup(s => s.AddSongToQueueAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Success");

        string result = await CreateHandler().HandleAddSongToQueueCommand(url);

        Assert.Equal("Added song to the queue...", result);
        _Search.Verify(s => s.GetSongsByName(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddSong_ServiceFails_ReturnsFailureMessage()
    {
        string url = "https://open.spotify.com/track/abc123";
        _Spotify.Setup(s => s.AddSongToQueueAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Error");

        string result = await CreateHandler().HandleAddSongToQueueCommand(url);

        Assert.Equal("Could not add song to the queue...", result);
    }

    [Fact]
    public async Task AddSong_InvalidInput_ReturnsValidLinkMessage()
    {
        string result = await CreateHandler().HandleAddSongToQueueCommand("just a song name");

        Assert.Equal("Please provide a valid spotify link", result);
        _Spotify.Verify(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── HandlePlaySpecificSongCommand ────────────────────────────────────────

    [Fact]
    public async Task PlaySpecificSong_AdminUser_PlaysAndReturnsMessage()
    {
        _Spotify.Setup(s => s.PlaySpecificSongAsync("songuri", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        string result = await CreateHandler().HandlePlaySpecificSongCommand("songuri", "itsspekkie");

        Assert.Contains("songuri", result);
        Assert.Contains("started", result);
    }

    [Fact]
    public async Task PlaySpecificSong_NonAdminUser_ReturnsEmpty()
    {
        string result = await CreateHandler().HandlePlaySpecificSongCommand("songuri", "randomviewer");

        Assert.Equal("", result);
        _Spotify.Verify(s => s.PlaySpecificSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaySpecificSong_AdminUserCaseInsensitive_Allowed()
    {
        _Spotify.Setup(s => s.PlaySpecificSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        string result = await CreateHandler().HandlePlaySpecificSongCommand("song", "ITSSPEKKIE");

        Assert.NotEqual("", result);
    }

    // ── Simple delegate handlers ─────────────────────────────────────────────

    [Fact]
    public async Task PauseMusic_Success_ReturnsPausedMessage()
    {
        _Spotify.Setup(s => s.PausePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        string result = await CreateHandler().HandlePauseMusicCommand();
        Assert.Equal("Player paused...", result);
    }

    [Fact]
    public async Task PauseMusic_Failure_ReturnsErrorMessage()
    {
        _Spotify.Setup(s => s.PausePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        string result = await CreateHandler().HandlePauseMusicCommand();
        Assert.Equal("Player not paused due to an error...", result);
    }

    [Fact]
    public async Task ResumeMusic_Success_ReturnsResumedMessage()
    {
        _Spotify.Setup(s => s.ResumePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        string result = await CreateHandler().HandleResumeMusicCommand();
        Assert.Equal("Player resumed...", result);
    }
}
