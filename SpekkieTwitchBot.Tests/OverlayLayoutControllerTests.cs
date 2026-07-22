using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Overlay;

namespace SpekkieTwitchBot.Tests;

public class OverlayLayoutControllerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"layout-mode-override-{Guid.NewGuid():N}.txt");

    private OverlayLayoutController Create() => Create(out _);

    private OverlayLayoutController Create(out Mock<Logger> logger)
    {
        logger = new Mock<Logger>(MockBehavior.Loose, null!);
        return new OverlayLayoutController(logger.Object, _path);
    }

    private void WriteOverride(string content) => File.WriteAllText(_path, content);

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // ── Manual override wins ───────────────────────────────────────────────────

    [Theory]
    [InlineData("clashLandscape")]
    [InlineData("fullHdGame")]
    [InlineData("ipadPortrait")]
    public void Resolve_ManualLayout_ReturnsThatLayout(string layout)
    {
        WriteOverride(layout);
        Assert.Equal(layout, Create().Resolve());
    }

    [Fact]
    public void Resolve_ManualLayout_IsCaseInsensitiveAndTrimmed()
    {
        WriteOverride("  IPADPORTRAIT \n");
        Assert.Equal("ipadPortrait", Create().Resolve());
    }

    // The browser overlay matches the camelCase layout name, so resolution must return the canonical
    // casing regardless of how the override file is cased.
    [Fact]
    public void Resolve_ManualLayout_ReturnsCanonicalCasing()
    {
        WriteOverride("  FULLHDGAME \n");
        Assert.Equal("fullHdGame", Create().Resolve());
    }

    // ── Default fallback ────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_Auto_ReturnsDefault()
    {
        WriteOverride("auto");
        Assert.Equal("clashLandscape", Create().Resolve());
    }

    [Fact]
    public void Resolve_EmptyFile_FallsBackToDefault()
    {
        WriteOverride("   ");
        Assert.Equal("clashLandscape", Create().Resolve());
    }

    [Fact]
    public void Resolve_MissingFile_FallsBackToDefault()
    {
        // no file written
        Assert.Equal("clashLandscape", Create().Resolve());
    }

    [Fact]
    public void Resolve_UnrecognizedContent_FallsBackToDefault()
    {
        WriteOverride("fullHd");
        Assert.Equal("clashLandscape", Create().Resolve());
    }

    // ── Logging ────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_UnrecognizedContent_LogsWarning()
    {
        WriteOverride("fullHd");
        OverlayLayoutController sut = Create(out Mock<Logger> logger);

        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("fullHd"))), Times.Once);
    }

    [Fact]
    public void Resolve_StandingTypo_WarnsOnlyOnce()
    {
        WriteOverride("fullHd");
        OverlayLayoutController sut = Create(out Mock<Logger> logger);

        sut.Resolve();
        sut.Resolve();
        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Resolve_AutoOrRecognized_DoesNotWarn()
    {
        WriteOverride("auto");
        OverlayLayoutController sut = Create(out Mock<Logger> logger);

        sut.Resolve();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }
}
