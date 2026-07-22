using EventTimerService;
using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class TimerCommandHandlerTests
{
    private readonly Mock<IEventTimerService> _TimerService = new();
    private readonly Mock<ITimerFileWriter> _TimerFileWriter = new();
    private readonly Mock<IFeatureFlagService> _FeatureFlags = new();
    private readonly Mock<IOverlayTimerWriter> _OverlayTimerWriter = new();

    private TimerCommandHandler CreateHandler() => new(_TimerService.Object, _TimerFileWriter.Object, _FeatureFlags.Object, _OverlayTimerWriter.Object);

    [Fact]
    public void HandleAddTimeToTimer_Seconds_AddsCorrectAmount()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromMinutes(10));

        string result = CreateHandler().HandleAddTimeToTimerCommand("30s");

        _TimerService.Verify(t => t.SetRemainingTime(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30)));
        Assert.Equal("added 30 seconds to timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_Minutes_AddsCorrectAmount()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromHours(1));

        string result = CreateHandler().HandleAddTimeToTimerCommand("5m");

        _TimerService.Verify(t => t.SetRemainingTime(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(5)));
        Assert.Equal("added 5 minutes to the timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_Hours_AddsCorrectAmount()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromMinutes(30));

        string result = CreateHandler().HandleAddTimeToTimerCommand("2h");

        _TimerService.Verify(t => t.SetRemainingTime(TimeSpan.FromMinutes(30) + TimeSpan.FromHours(2)));
        Assert.Equal("added 2 hours to the timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_NoSuffix_ReturnsUsageMessageAndDoesNotSetTime()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.Zero);

        string result = CreateHandler().HandleAddTimeToTimerCommand("42");

        _TimerService.Verify(t => t.SetRemainingTime(It.IsAny<TimeSpan>()), Times.Never);
        Assert.Contains("Usage", result);
    }

    [Fact]
    public void HandleSetTimeOnTimer_SetsCorrectTimeSpan()
    {
        CreateHandler().HandleSetTimeOnTimerCommand("01:30:00");

        _TimerService.Verify(t => t.SetRemainingTime(new TimeSpan(1, 30, 0)));
    }

    [Fact]
    public void HandleSetTimeOnTimer_ReturnsPaddedMessage()
    {
        string result = CreateHandler().HandleSetTimeOnTimerCommand("1:5:9");

        Assert.Equal("Set timer to 01:05:09", result);
    }

    [Fact]
    public async Task HandlePauseTimer_StopsTimerWritesOverlayFalseAndReturnsRemainingTime()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(0, 4, 30));

        string result = await CreateHandler().HandlePauseTimerCommand(CancellationToken.None);

        _TimerService.Verify(t => t.StopTimer());
        _OverlayTimerWriter.Verify(w => w.SetTimerRunningAsync(false, It.IsAny<CancellationToken>()));
        Assert.Contains("00:04:30", result);
    }

    [Fact]
    public async Task HandleStartTimer_StartsTimerWritesOverlayTrueAndReturnsRemainingTime()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(1, 0, 0));

        string result = await CreateHandler().HandleStartTimerCommand(CancellationToken.None);

        _TimerService.Verify(t => t.StartTimer());
        _OverlayTimerWriter.Verify(w => w.SetTimerRunningAsync(true, It.IsAny<CancellationToken>()));
        Assert.Contains("01:00:00", result);
    }

    [Fact]
    public void HandleSetTimeOnTimer_InvalidFormat_ReturnsUsageMessage()
    {
        string result = CreateHandler().HandleSetTimeOnTimerCommand("not-a-time");

        _TimerService.Verify(t => t.SetRemainingTime(It.IsAny<TimeSpan>()), Times.Never);
        Assert.Contains("Usage", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_InvalidNumber_ReturnsUsageMessage()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromMinutes(10));

        string result = CreateHandler().HandleAddTimeToTimerCommand("abcs");

        _TimerService.Verify(t => t.SetRemainingTime(It.IsAny<TimeSpan>()), Times.Never);
        Assert.Contains("Usage", result);
    }
}
