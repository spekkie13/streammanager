using Moq;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class ClashCommandHandlerTests
{
    private readonly Mock<IWarService> _War = new();
    private readonly Mock<IObsWebSocket> _Obs = new();
    private ClashCommandHandler CreateHandler() => new(_War.Object, _Obs.Object);

    [Fact]
    public void SetWarStats_WhenTurnedOn_ReturnsTurnedOnMessage()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", It.IsAny<string>(), 0)).Returns(1);

        string result = CreateHandler().HandleSetWarStatsCommand("on");

        Assert.Equal("War service has been turned on", result);
    }

    [Fact]
    public void SetWarStats_WhenTurnedOff_ReturnsTurnedOffMessage()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", It.IsAny<string>(), 0)).Returns(1);

        string result = CreateHandler().HandleSetWarStatsCommand("off");

        Assert.Equal("War service has been turned off", result);
    }

    [Fact]
    public void SetWarStats_WarOn_ChatboxHiddenBrowserShown()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId("Scene", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Scene", "War Stats", 0)).Returns(20);

        CreateHandler().HandleSetWarStatsCommand("on");

        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 10, false)); // chatbox hidden
        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 20, true));  // war stats shown
    }

    [Fact]
    public void SetWarStats_WarOff_ChatboxShownBrowserHidden()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId("Scene", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Scene", "War Stats", 0)).Returns(20);

        CreateHandler().HandleSetWarStatsCommand("off");

        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 10, true));  // chatbox shown
        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 20, false)); // war stats hidden
    }

    [Fact]
    public void SetWarStats_CallsSetWarStatsOnWarService()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId(It.IsAny<string>(), It.IsAny<string>(), 0)).Returns(1);

        CreateHandler().HandleSetWarStatsCommand("on");

        _War.Verify(w => w.SetWarStats(true));
    }

    [Fact]
    public void SetWarStats_InvalidArgument_ReturnsUsageMessage()
    {
        string result = CreateHandler().HandleSetWarStatsCommand("invalid");

        Assert.Equal("Usage: !war on | !war off", result);
    }

    [Fact]
    public void AddPlayerTag_UpdatesAndReturnsConfirmation()
    {
        string result = CreateHandler().HandleAddPlayerTagCommand("#ABC123");

        _War.Verify(w => w.UpdatePlayerTag("#ABC123"));
        Assert.Contains("#ABC123", result);
    }
}
