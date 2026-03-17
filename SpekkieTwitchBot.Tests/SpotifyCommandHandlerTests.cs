using Moq;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Tests;

public class SpotifyCommandHandlerTests
{
    private readonly Mock<ISpotifyService> _spotify = new();
    private readonly Mock<SpotifyFileWriter> _fileWriter = new(MockBehavior.Loose, null!);
    private readonly Mock<ISpotifySearchService> _search = new();

    private SpotifyCommandHandler CreateHandler() =>
        new(_spotify.Object, _fileWriter.Object, _search.Object);

    // ── HandleAddSongToQueueCommand ──────────────────────────────────────────

    [Fact]
    public async Task AddSong_TitleArtistFormat_SearchesAndQueues()
    {
        Track track = new() { Uri = "spotify:track:abc123" };
        _search.Setup(s => s.GetSongsByName("Song", "Artist")).ReturnsAsync(track);
        _spotify.Setup(s => s.AddSongToQueueAsync("spotify:track:abc123", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Success");

        string result = await CreateHandler().HandleAddSongToQueueCommand("Song|Artist");

        Assert.Equal("Added song to the queue...", result);
    }

    [Fact]
    public async Task AddSong_TitleArtistFormat_SearchFails_ReturnsValidLinkMessage()
    {
        _search.Setup(s => s.GetSongsByName("Song", "Artist")).ReturnsAsync((Track?)null);

        string result = await CreateHandler().HandleAddSongToQueueCommand("Song|Artist");

        Assert.Equal("Please provide a valid spotify link", result);
    }

    [Fact]
    public async Task AddSong_SpotifyUrl_PassesDirectlyToService()
    {
        string url = "https://open.spotify.com/track/abc123";
        _spotify.Setup(s => s.AddSongToQueueAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Success");

        string result = await CreateHandler().HandleAddSongToQueueCommand(url);

        Assert.Equal("Added song to the queue...", result);
        _search.Verify(s => s.GetSongsByName(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddSong_ServiceFails_ReturnsFailureMessage()
    {
        string url = "https://open.spotify.com/track/abc123";
        _spotify.Setup(s => s.AddSongToQueueAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Error");

        string result = await CreateHandler().HandleAddSongToQueueCommand(url);

        Assert.Equal("Could not add song to the queue...", result);
    }

    [Fact]
    public async Task AddSong_InvalidInput_ReturnsValidLinkMessage()
    {
        string result = await CreateHandler().HandleAddSongToQueueCommand("just a song name");

        Assert.Equal("Please provide a valid spotify link", result);
        _spotify.Verify(s => s.AddSongToQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── HandlePlaySpecificSongCommand ────────────────────────────────────────

    [Fact]
    public async Task PlaySpecificSong_AdminUser_PlaysAndReturnsMessage()
    {
        _spotify.Setup(s => s.PlaySpecificSongAsync("songuri", It.IsAny<CancellationToken>()))
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
        _spotify.Verify(s => s.PlaySpecificSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaySpecificSong_AdminUserCaseInsensitive_Allowed()
    {
        _spotify.Setup(s => s.PlaySpecificSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        string result = await CreateHandler().HandlePlaySpecificSongCommand("song", "ITSSPEKKIE");

        Assert.NotEqual("", result);
    }

    // ── Simple delegate handlers ─────────────────────────────────────────────

    [Fact]
    public async Task PauseMusic_Success_ReturnsPausedMessage()
    {
        _spotify.Setup(s => s.PausePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        string result = await CreateHandler().HandlePauseMusicCommand();
        Assert.Equal("Player paused...", result);
    }

    [Fact]
    public async Task PauseMusic_Failure_ReturnsErrorMessage()
    {
        _spotify.Setup(s => s.PausePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        string result = await CreateHandler().HandlePauseMusicCommand();
        Assert.Equal("Player not paused due to an error...", result);
    }

    [Fact]
    public async Task ResumeMusic_Success_ReturnsResumedMessage()
    {
        _spotify.Setup(s => s.ResumePlayerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        string result = await CreateHandler().HandleResumeMusicCommand();
        Assert.Equal("Player resumed...", result);
    }
}
