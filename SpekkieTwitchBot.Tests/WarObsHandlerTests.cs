using Moq;
using SpekkieClassLibrary.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.OBS.Websocket;

namespace SpekkieTwitchBot.Tests;

// "Chatbox" / "War Stats" only exist in some scenes. When the active scene lacks them OBS answers with
// ErrorCode 600, which used to escape the war-state handler and surface as an EventBus error on every
// boot and every war-state transition.
public class WarObsHandlerTests
{
    private readonly Mock<IObsWebSocket> _Obs = new();
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);
    private readonly WarStatus _Status = new();
    private readonly FakeEventBus _Bus = new();

    private void Register()
    {
        _Status.Mode = WarDisplayMode.Auto;
        new WarObsHandler(_Obs.Object, _Bus, _Status, _Logger.Object).Register();
    }

    [Fact]
    public async Task MissingSceneItem_DoesNotThrow()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Starting Soon");
        _Obs.Setup(o => o.GetSceneItemId("Starting Soon", It.IsAny<string>(), 0))
            .Throws(new ErrorResponseException("No scene items were found", 600));

        Register();

        await _Bus.PublishAsync(new WarStateChangedEvent("inWar", "us", "them"));

        _Obs.Verify(o => o.SetSceneItemEnabled(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    // A scene that only has one of the two sources still gets the other one toggled.
    [Fact]
    public async Task MissingOneItem_StillTogglesTheOther()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", "Chatbox", 0))
            .Throws(new ErrorResponseException("No scene items were found", 600));
        _Obs.Setup(o => o.GetSceneItemId("Gaming", "War Stats", 0)).Returns(20);

        Register();

        await _Bus.PublishAsync(new WarStateChangedEvent("inWar", "us", "them"));

        _Obs.Verify(o => o.SetSceneItemEnabled("Gaming", 20, true), Times.Once);
    }

    [Fact]
    public async Task BothItemsPresent_TogglesChatboxOffAndWarStatsOn()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Returns("Gaming");
        _Obs.Setup(o => o.GetSceneItemId("Gaming", "Chatbox", 0)).Returns(10);
        _Obs.Setup(o => o.GetSceneItemId("Gaming", "War Stats", 0)).Returns(20);

        Register();

        await _Bus.PublishAsync(new WarStateChangedEvent("inWar", "us", "them"));

        _Obs.Verify(o => o.SetSceneItemEnabled("Gaming", 10, false), Times.Once);
        _Obs.Verify(o => o.SetSceneItemEnabled("Gaming", 20, true), Times.Once);
    }

    // OBS being unreachable must not take the handler down either.
    [Fact]
    public async Task SceneLookupFails_DoesNotThrow()
    {
        _Obs.Setup(o => o.GetCurrentProgramScene()).Throws(new InvalidOperationException("not connected"));

        Register();

        await _Bus.PublishAsync(new WarStateChangedEvent("notInWar", null, null));

        _Obs.Verify(o => o.SetSceneItemEnabled(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    private sealed class FakeEventBus : IStreamEventBus
    {
        private readonly List<Delegate> _Handlers = [];

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) => _Handlers.Add(handler);

        public async Task PublishAsync<TEvent>(TEvent e, CancellationToken ct = default)
        {
            foreach (Func<TEvent, CancellationToken, Task> handler in _Handlers.OfType<Func<TEvent, CancellationToken, Task>>())
                await handler(e, ct);
        }
    }
}
