using Moq;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class ObsCommandHandlerTests
{
    private readonly Mock<IObsWebSocket> _obs = new();
    private ObsCommandHandler CreateHandler() => new(_obs.Object);

    // ── HandleSetSceneCommand ────────────────────────────────────────────────

    [Fact]
    public void SetScene_CapitalisesFirstLetter()
    {
        string? sceneSent = null;
        _obs.Setup(o => o.SetCurrentProgramScene(It.IsAny<string>()))
            .Callback<string>(s => sceneSent = s);

        CreateHandler().HandleSetSceneCommand("gaming");

        Assert.Equal("Gaming", sceneSent);
    }

    [Fact]
    public void SetScene_ReturnsMessageWithCapitalisedName()
    {
        string result = CreateHandler().HandleSetSceneCommand("coding");

        Assert.Equal("Changing scene to Coding", result);
    }

    [Fact]
    public void SetScene_AlreadyCapitalised_StaysUnchanged()
    {
        string? sceneSent = null;
        _obs.Setup(o => o.SetCurrentProgramScene(It.IsAny<string>()))
            .Callback<string>(s => sceneSent = s);

        CreateHandler().HandleSetSceneCommand("Gaming");

        Assert.Equal("Gaming", sceneSent);
    }

    // ── HandleSetInputMute ───────────────────────────────────────────────────

    [Fact]
    public void SetInputMute_WhenCurrentlyMuted_ReturnsUnmutedMessage()
    {
        _obs.Setup(o => o.GetInputMute("Microphone")).Returns(true);

        string result = CreateHandler().HandleSetInputMute("microphone");

        _obs.Verify(o => o.SetInputMute("Microphone", false));
        Assert.Equal("Microphone set to unmuted", result);
    }

    [Fact]
    public void SetInputMute_WhenCurrentlyUnmuted_ReturnsMutedMessage()
    {
        _obs.Setup(o => o.GetInputMute("Microphone")).Returns(false);

        string result = CreateHandler().HandleSetInputMute("microphone");

        _obs.Verify(o => o.SetInputMute("Microphone", true));
        Assert.Equal("Microphone set to muted", result);
    }

    // ── HandleSetStandardVolumes ─────────────────────────────────────────────

    [Fact]
    public void SetStandardVolumes_CallsSetVolumeForAllInputsAndOutputs()
    {
        _obs.Setup(o => o.GetInputList("wasapi_input_capture"))
            .Returns([new InputBasicInfo { InputName = "Mic" }]);
        _obs.Setup(o => o.GetInputList("wasapi_output_capture"))
            .Returns([new InputBasicInfo { InputName = "Desktop" }]);

        string result = CreateHandler().HandleSetStandardVolumes();

        _obs.Verify(o => o.SetInputVolume("Mic", It.IsAny<float>(), true));
        _obs.Verify(o => o.SetInputVolume("Desktop", It.IsAny<float>(), true));
        Assert.Equal("Set standard volumes", result);
    }

    // ── HandleVolumeZero ─────────────────────────────────────────────────────

    [Fact]
    public void VolumeZero_SetsAllInputsToZero()
    {
        _obs.Setup(o => o.GetInputList("wasapi_input_capture"))
            .Returns([new InputBasicInfo { InputName = "Mic" }]);
        _obs.Setup(o => o.GetInputList("wasapi_output_capture"))
            .Returns([new InputBasicInfo { InputName = "Desktop" }]);

        string result = CreateHandler().HandleVolumeZero();

        _obs.Verify(o => o.SetInputVolume("Mic", 0.0f, false));
        _obs.Verify(o => o.SetInputVolume("Desktop", 0.0f, false));
        Assert.Equal("All inputs & outputs muted", result);
    }
}
