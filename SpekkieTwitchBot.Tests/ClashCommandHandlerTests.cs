using Moq;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.OBS.Websocket;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class ClashCommandHandlerTests
{
    private readonly Mock<IWarService> _War = new();
    private readonly Mock<IObsWebSocket> _Obs = new();
    private ClashCommandHandler CreateHandler() => new(_War.Object, _Obs.Object);

    [Fact]
    public void SetWarStats_WhenForcedOn_ReturnsForcedOnMessage()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", It.IsAny<string>(), 0)).Returns(1);

        string result = CreateHandler().HandleSetWarStatsCommand("on");

        Assert.Equal("War stats forced on", result);
    }

    [Fact]
    public void SetWarStats_WhenForcedOff_ReturnsForcedOffMessage()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", It.IsAny<string>(), 0)).Returns(1);

        string result = CreateHandler().HandleSetWarStatsCommand("off");

        Assert.Equal("War stats forced off", result);
    }

    [Fact]
    public void SetWarStats_WhenAuto_ReturnsAutoMessage()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", It.IsAny<string>(), 0)).Returns(1);
        _War.Setup(w => w.IsWarActive).Returns(false);

        string result = CreateHandler().HandleSetWarStatsCommand("auto");

        Assert.Equal("War stats set to auto mode", result);
    }

    [Fact]
    public void SetWarStats_ForceOn_ChatboxHiddenWarStatsShown()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId("Scene", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Scene", "War Stats", 0)).Returns(20);

        CreateHandler().HandleSetWarStatsCommand("on");

        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 10, false)); // chatbox hidden
        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 20, true));  // war stats shown
    }

    [Fact]
    public void SetWarStats_ForceOff_ChatboxShownWarStatsHidden()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId("Scene", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Scene", "War Stats", 0)).Returns(20);

        CreateHandler().HandleSetWarStatsCommand("off");

        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 10, true));  // chatbox shown
        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 20, false)); // war stats hidden
    }

    [Fact]
    public void SetWarStats_Auto_ObsReflectsIsWarActive()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId("Scene", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Scene", "War Stats", 0)).Returns(20);
        _War.Setup(w => w.IsWarActive).Returns(true);

        CreateHandler().HandleSetWarStatsCommand("auto");

        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 10, false)); // chatbox hidden (war active)
        _Obs.Verify(o => o.SetSceneItemEnabled("Scene", 20, true));  // war stats shown (war active)
    }

    [Fact]
    public void SetWarStats_CallsSetWarModeOnWarService()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Scene");
        _Obs.Setup(o => o.GetSceneItemId(It.IsAny<string>(), It.IsAny<string>(), 0)).Returns(1);

        CreateHandler().HandleSetWarStatsCommand("on");

        _War.Verify(w => w.SetWarMode(WarDisplayMode.ForceOn));
    }

    [Fact]
    public void SetWarStats_InvalidArgument_ReturnsUsageMessage()
    {
        string result = CreateHandler().HandleSetWarStatsCommand("invalid");

        Assert.Equal("Usage: !war on | !war off | !war auto", result);
    }

    [Fact]
    public async Task AddPlayerTag_UpdatesAndReturnsConfirmation()
    {
        _War.Setup(w => w.UpdatePlayerTag("#ABC123")).Returns(Task.CompletedTask);

        string result = await CreateHandler().HandleAddPlayerTagCommand("#ABC123");

        _War.Verify(w => w.UpdatePlayerTag("#ABC123"));
        Assert.Contains("#ABC123", result);
    }
}
