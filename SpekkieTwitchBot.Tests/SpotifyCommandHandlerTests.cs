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
    private readonly Mock<ITwitchChannelInfoClient> _ChannelInfo = new();

    private SpotifyCommandHandler CreateHandler() =>
        new(_Spotify.Object, _FileWriter.Object, _ChannelInfo.Object);

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
