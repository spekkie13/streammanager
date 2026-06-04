using Moq;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Overlay;

namespace SpekkieTwitchBot.Tests;

public class OverlayModeControllerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"overlay-mode-override-{Guid.NewGuid():N}.txt");

    private OverlayModeController Create(bool warActive) => Create(warActive, out _);

    private OverlayModeController Create(bool warActive, out Mock<Logger> logger)
    {
        Mock<IWarService> war = new();
        war.SetupGet(w => w.IsWarActive).Returns(warActive);
        logger = new Mock<Logger>(MockBehavior.Loose, null!);
        return new OverlayModeController(war.Object, logger.Object, _path);
    }

    private void WriteOverride(string content) => File.WriteAllText(_path, content);

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // ── Manual override wins ───────────────────────────────────────────────────

    [Theory]
    [InlineData("war")]
    [InlineData("farming")]
    [InlineData("event")]
    public void Resolve_ManualMode_ReturnsThatMode(string mode)
    {
        WriteOverride(mode);
        Assert.Equal(mode, Create(warActive: true).Resolve());
    }

    [Fact]
    public void Resolve_ManualMode_IsCaseInsensitiveAndTrimmed()
    {
        WriteOverride("  EVENT \n");
        Assert.Equal("event", Create(warActive: false).Resolve());
    }

    // ── Automatic mode ─────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_AutoAndWarActive_ReturnsWar()
    {
        WriteOverride("auto");
        Assert.Equal("war", Create(warActive: true).Resolve());
    }

    [Fact]
    public void Resolve_AutoAndNoWar_ReturnsFarming()
    {
        WriteOverride("auto");
        Assert.Equal("farming", Create(warActive: false).Resolve());
    }

    [Fact]
    public void Resolve_EmptyFile_FallsBackToAuto()
    {
        WriteOverride("   ");
        Assert.Equal("war", Create(warActive: true).Resolve());
    }

    [Fact]
    public void Resolve_MissingFile_FallsBackToAuto()
    {
        // no file written
        Assert.Equal("farming", Create(warActive: false).Resolve());
    }

    [Fact]
    public void Resolve_UnrecognizedContent_FallsBackToAuto()
    {
        WriteOverride("farmign");
        Assert.Equal("farming", Create(warActive: false).Resolve());
    }

    // ── Logging ────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_UnrecognizedContent_LogsWarning()
    {
        WriteOverride("farmin");
        OverlayModeController sut = Create(warActive: false, out Mock<Logger> logger);

        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("farmin"))), Times.Once);
    }

    [Fact]
    public void Resolve_StandingTypo_WarnsOnlyOnce()
    {
        WriteOverride("farmin");
        OverlayModeController sut = Create(warActive: false, out Mock<Logger> logger);

        sut.Resolve();
        sut.Resolve();
        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_AutoOrRecognized_DoesNotWarn()
    {
        WriteOverride("auto");
        OverlayModeController sut = Create(warActive: true, out Mock<Logger> logger);

        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }
}