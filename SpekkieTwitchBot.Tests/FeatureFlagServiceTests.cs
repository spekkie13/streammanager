using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.General;

namespace SpekkieTwitchBot.Tests;

public class FeatureFlagServiceTests : IDisposable
{
    private readonly string _FlagsPath =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_features.json");

    private readonly Mock<Logger> _Logger;

    public FeatureFlagServiceTests()
    {
        Mock<ITextFileWriter> textWriter = new();
        Mock<GeneralFileWriter> fileWriter = new(textWriter.Object);
        _Logger = new Mock<Logger>(fileWriter.Object);
    }

    public void Dispose()
    {
        if (File.Exists(_FlagsPath)) File.Delete(_FlagsPath);
    }

    private FeatureFlagService Create() => new(_Logger.Object, _FlagsPath);

    private async Task<FeatureFlagService> CreateWithJson(string json)
    {
        await File.WriteAllTextAsync(_FlagsPath, json);
        FeatureFlagService svc = Create();
        await svc.InitializeAsync();
        return svc;
    }

    [Fact]
    public async Task IsEnabled_TrueFlag_ReturnsTrue()
    {
        using FeatureFlagService svc = await CreateWithJson("""{"Marathon": true}""");
        Assert.True(svc.IsEnabled("Marathon"));
    }

    [Fact]
    public async Task IsEnabled_FalseFlag_ReturnsFalse()
    {
        using FeatureFlagService svc = await CreateWithJson("""{"Marathon": false}""");
        Assert.False(svc.IsEnabled("Marathon"));
    }

    [Fact]
    public async Task IsEnabled_MissingKey_ReturnsFalse()
    {
        using FeatureFlagService svc = await CreateWithJson("""{"War": true}""");
        Assert.False(svc.IsEnabled("Marathon"));
    }

    [Fact]
    public async Task IsEnabled_NoFile_ReturnsFalse()
    {
        using FeatureFlagService svc = Create();
        await svc.InitializeAsync();
        Assert.False(svc.IsEnabled("Marathon"));
    }

    [Fact]
    public async Task IsEnabled_InvalidJsonOnReload_RetainsOldValues()
    {
        using FeatureFlagService svc = await CreateWithJson("""{"Marathon": true}""");

        await File.WriteAllTextAsync(_FlagsPath, "NOT_JSON");
        await Task.Delay(700); // wait for debounce (500ms) + read margin

        Assert.True(svc.IsEnabled("Marathon"));
        _Logger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
    }
}
