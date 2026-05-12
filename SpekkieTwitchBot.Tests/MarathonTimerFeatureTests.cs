using EventTimerService;
using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Tests;

public class MarathonTimerFeatureTests
{
    private readonly Mock<IEventTimerService> _Timer = new();
    private readonly Mock<IMarathonTimeCalculator> _Calculator = new();
    private readonly Mock<ITwitchChat> _Chat = new();
    private readonly Mock<Logger> _Logger;
    private readonly Mock<IFeatureFlagService> _Flags = new();

    public MarathonTimerFeatureTests()
    {
        Mock<ITextFileWriter> textWriter = new();
        Mock<GeneralFileWriter> fileWriter = new(textWriter.Object);
        _Logger = new Mock<Logger>(fileWriter.Object);
    }

    private MarathonTimerFeature CreateFeature() =>
        new(_Timer.Object, _Calculator.Object, _Chat.Object, _Logger.Object, _Flags.Object);

    private static SubHappened Sub(SubKind kind = SubKind.New, string recipient = "viewer1",
        string? gifter = null, string tier = "1000") =>
        new(kind, "uid1", recipient, null, gifter, tier, false, null, 0, null, DateTimeOffset.UtcNow);

    private static BitsHappened Bits(int bits = 500) =>
        new("uid1", "viewer1", false, bits, null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task HandleSubAsync_FlagDisabled_DoesNotAddTime()
    {
        _Flags.Setup(f => f.IsEnabled("Marathon")).Returns(false);
        _Calculator.Setup(c => c.CalculateForSub(It.IsAny<SubHappened>())).Returns(TimeSpan.FromMinutes(5));

        await CreateFeature().HandleSubAsync(Sub(), CancellationToken.None);

        _Timer.Verify(t => t.AddTime(It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task HandleBitsAsync_FlagDisabled_DoesNotAddTime()
    {
        _Flags.Setup(f => f.IsEnabled("Marathon")).Returns(false);
        _Calculator.Setup(c => c.CalculateForBits(It.IsAny<int>())).Returns(TimeSpan.FromMinutes(3));

        await CreateFeature().HandleBitsAsync(Bits(), CancellationToken.None);

        _Timer.Verify(t => t.AddTime(It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task HandleDonationAsync_FlagDisabled_DoesNotAddTime()
    {
        _Flags.Setup(f => f.IsEnabled("Marathon")).Returns(false);
        _Calculator.Setup(c => c.CalculateForDonation(It.IsAny<decimal>())).Returns(TimeSpan.FromMinutes(2));

        await CreateFeature().HandleDonationAsync("viewer1", 5m, CancellationToken.None);

        _Timer.Verify(t => t.AddTime(It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task HandleSubAsync_FlagEnabled_AddsTime()
    {
        _Flags.Setup(f => f.IsEnabled("Marathon")).Returns(true);
        _Calculator.Setup(c => c.CalculateForSub(It.IsAny<SubHappened>())).Returns(TimeSpan.FromMinutes(5));
        _Chat.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await CreateFeature().HandleSubAsync(Sub(), CancellationToken.None);

        _Timer.Verify(t => t.AddTime(TimeSpan.FromMinutes(5)), Times.Once);
    }
}
