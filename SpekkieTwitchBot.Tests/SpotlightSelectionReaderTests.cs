using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Overlay;

namespace SpekkieTwitchBot.Tests;

public class SpotlightSelectionReaderTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"spotlight-selection-{Guid.NewGuid():N}.txt");

    private SpotlightSelectionReader Create() => Create(out _);

    private SpotlightSelectionReader Create(out Mock<Logger> logger)
    {
        logger = new Mock<Logger>(MockBehavior.Loose, null!);
        return new SpotlightSelectionReader(logger.Object, _path);
    }

    private void WriteSelection(string content) => File.WriteAllText(_path, content);

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // ── Valid selections ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("home:1", "home", 1)]
    [InlineData("home:5", "home", 5)]
    [InlineData("away:2", "away", 2)]
    public void Read_ValidSelection_ReturnsTeamAndPosition(string content, string team, int position)
    {
        WriteSelection(content);
        SpotlightSelection? selection = Create().Read();
        Assert.Equal(new SpotlightSelection(team, position), selection);
    }

    [Fact]
    public void Read_Selection_IsCaseInsensitiveAndTrimmed()
    {
        WriteSelection("  HOME:3 \n");
        Assert.Equal(new SpotlightSelection("home", 3), Create().Read());
    }

    // ── Off / empty / missing ──────────────────────────────────────────────────

    [Theory]
    [InlineData("off")]
    [InlineData("OFF")]
    [InlineData("   ")]
    public void Read_OffOrEmpty_ReturnsNull(string content)
    {
        WriteSelection(content);
        Assert.Null(Create().Read());
    }

    [Fact]
    public void Read_MissingFile_ReturnsNull()
    {
        // no file written
        Assert.Null(Create().Read());
    }

    // ── Invalid values become "off" (null) ─────────────────────────────────────

    [Theory]
    [InlineData("home:0")]   // below range
    [InlineData("home:6")]   // above 5v5 range
    [InlineData("home:abc")] // non-numeric
    [InlineData("enemy:1")]  // unknown team
    [InlineData("home")]     // no position
    [InlineData("home:1:2")] // garbage
    public void Read_InvalidSelection_ReturnsNull(string content)
    {
        WriteSelection(content);
        Assert.Null(Create().Read());
    }

    // ── Logging ────────────────────────────────────────────────────────────────

    [Fact]
    public void Read_InvalidSelection_LogsWarning()
    {
        WriteSelection("home:9");
        SpotlightSelectionReader sut = Create(out Mock<Logger> logger);

        sut.Read();

        logger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("home:9"))), Times.Once);
    }

    [Fact]
    public void Read_StandingInvalidSelection_WarnsOnlyOnce()
    {
        WriteSelection("home:9");
        SpotlightSelectionReader sut = Create(out Mock<Logger> logger);

        sut.Read();
        sut.Read();
        sut.Read();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Read_ValidOrOff_DoesNotWarn()
    {
        WriteSelection("away:4");
        SpotlightSelectionReader sut = Create(out Mock<Logger> logger);

        sut.Read();

        logger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }
}
