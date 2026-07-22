using EventTimerService;
using Moq;
using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class AfkCommandHandlerTests
{
    private readonly Mock<IEventTimerService> _TimerService = new();
    private readonly Mock<IOverlayAfkWriter> _AfkWriter = new();

    private AfkCommandHandler CreateHandler() => new(_TimerService.Object, _AfkWriter.Object);

    [Fact]
    public async Task HandleAfk_PausesTimerSetsAfkTrueAndTimerRunningFalse()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(1, 0, 0));

        string result = await CreateHandler().HandleAfkCommand(CancellationToken.None);

        _TimerService.Verify(t => t.StopTimer());
        _AfkWriter.Verify(w => w.SetAfkAsync(true, false, It.IsAny<CancellationToken>()));
        Assert.Contains("AFK", result);
    }

    [Fact]
    public async Task HandleBack_ResumesTimerSetsAfkFalseAndTimerRunningTrue()
    {
        _TimerService.Setup(t => t.GetRemainingTime()).Returns(new TimeSpan(1, 0, 0));

        string result = await CreateHandler().HandleBackCommand(CancellationToken.None);

        _TimerService.Verify(t => t.StartTimer());
        _AfkWriter.Verify(w => w.SetAfkAsync(false, true, It.IsAny<CancellationToken>()));
        Assert.Contains("back", result);
    }
}
