using EventTimerService;
using Moq;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class TimerCommandHandlerTests
{
    private readonly Mock<IEventTimerService> _timerService = new();
    private readonly Mock<ITimerFileWriter> _timerFileWriter = new();
    private TimerCommandHandler CreateHandler() => new(_timerService.Object, _timerFileWriter.Object);

    [Fact]
    public void HandleAddTimeToTimer_Seconds_AddsCorrectAmount()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromMinutes(10));

        string result = CreateHandler().HandleAddTimeToTimerCommand("30s");

        _timerService.Verify(t => t.SetRemainingTime(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(30)));
        Assert.Equal("added 30 seconds to timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_Minutes_AddsCorrectAmount()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromHours(1));

        string result = CreateHandler().HandleAddTimeToTimerCommand("5m");

        _timerService.Verify(t => t.SetRemainingTime(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(5)));
        Assert.Equal("added 5 minutes to the timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_Hours_AddsCorrectAmount()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.FromMinutes(30));

        string result = CreateHandler().HandleAddTimeToTimerCommand("2h");

        _timerService.Verify(t => t.SetRemainingTime(TimeSpan.FromMinutes(30) + TimeSpan.FromHours(2)));
        Assert.Equal("added 2 hours to the timer", result);
    }

    [Fact]
    public void HandleAddTimeToTimer_NoSuffix_ReturnsEmptyAndDoesNotSetTime()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(TimeSpan.Zero);

        string result = CreateHandler().HandleAddTimeToTimerCommand("42");

        _timerService.Verify(t => t.SetRemainingTime(It.IsAny<TimeSpan>()), Times.Never);
        Assert.Equal("", result);
    }

    [Fact]
    public void HandleSetTimeOnTimer_SetsCorrectTimeSpan()
    {
        CreateHandler().HandleSetTimeOnTimerCommand("01:30:00");

        _timerService.Verify(t => t.SetRemainingTime(new TimeSpan(1, 30, 0)));
    }

    [Fact]
    public void HandleSetTimeOnTimer_ReturnsPaddedMessage()
    {
        string result = CreateHandler().HandleSetTimeOnTimerCommand("1:5:9");

        Assert.Equal("Set timer to 01:05:09", result);
    }

    [Fact]
    public void HandlePauseTimer_StopsTimerAndReturnsRemainingTime()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(0, 4, 30));

        string result = CreateHandler().HandlePauseTimerCommand();

        _timerService.Verify(t => t.StopTimer());
        Assert.Contains("00:04:30", result);
    }

    [Fact]
    public void HandleStartTimer_StartsTimerAndReturnsRemainingTime()
    {
        _timerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(1, 0, 0));

        string result = CreateHandler().HandleStartTimerCommand();

        _timerService.Verify(t => t.StartTimer());
        Assert.Contains("01:00:00", result);
    }

    [Fact]
    public void HandleSetTimerCommand_WritesTimeToFile()
    {
        CreateHandler().HandleSetTimerCommand("02:00:00");

        _timerFileWriter.Verify(w => w.WriteRemainingTime(new TimeSpan(2, 0, 0)));
    }
}
