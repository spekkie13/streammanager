using Moq;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class GeneralCommandHandlerTests
{
    private readonly Mock<GeneralFileReader> _Reader = new(MockBehavior.Loose, null!);
    private readonly Mock<GeneralFileWriter> _Writer = new(MockBehavior.Loose, null!);
    private readonly Mock<ITextCommandHandler> _Text = new();
    private readonly Mock<SpotifyCommandHandler> _Spotify = new(MockBehavior.Loose, null!, null!, null!);
    private readonly Mock<ObsCommandHandler> _Obs = new(MockBehavior.Loose, null!);
    private readonly Mock<TimerCommandHandler> _Timer = new(MockBehavior.Loose, null!, null!);
    private readonly Mock<TwitchCommandHandler> _Twitch = new(MockBehavior.Loose, null!);
    private readonly Mock<ClashCommandHandler> _Clash = new(MockBehavior.Loose, null!, null!);

    private GeneralCommandHandler CreateHandler() => new(
        _Reader.Object, _Writer.Object,
        _Text.Object, _Spotify.Object, _Obs.Object,
        _Timer.Object, _Twitch.Object, _Clash.Object);

    private static ChatCommandReceived Cmd(string command, string args = "", string user = "viewer") =>
        new("mid", "uid", user, command, args, $"!{command} {args}");

    [Fact]
    public async Task HandleCommand_UnknownCommand_ReturnsUnknownMessage()
    {
        string result = await CreateHandler().HandleCommand(Cmd("doesnotexist"), CancellationToken.None);
        Assert.Equal("Unknown command", result);
    }

    [Fact]
    public async Task HandleCommand_CaseInsensitive_MatchesCommand()
    {
        _Reader.Setup(r => r.ReadAfgeleidCounter()).Returns("5");

        string result = await CreateHandler().HandleCommand(Cmd("AFGELEID"), CancellationToken.None);

        Assert.Contains("6", result);
    }

    [Fact]
    public async Task HandleAfgeleid_IncrementsCounterAndReturnsMessage()
    {
        _Reader.Setup(r => r.ReadAfgeleidCounter()).Returns("3");

        string result = await CreateHandler().HandleCommand(Cmd("afgeleid"), CancellationToken.None);

        _Writer.Verify(w => w.WriteAfgeleidCounter("4"));
        Assert.Equal("Spekkie is 4x afgeleid geweest", result);
    }
}
