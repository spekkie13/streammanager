using Moq;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class GeneralCommandHandlerTests
{
    private readonly Mock<GeneralFileReader> _reader = new(MockBehavior.Loose, null!);
    private readonly Mock<GeneralFileWriter> _writer = new(MockBehavior.Loose, null!);
    private readonly Mock<ITextCommandHandler> _text = new();
    private readonly Mock<SpotifyCommandHandler> _spotify = new(MockBehavior.Loose, null!, null!, null!);
    private readonly Mock<ObsCommandHandler> _obs = new(MockBehavior.Loose, null!);
    private readonly Mock<TimerCommandHandler> _timer = new(MockBehavior.Loose, null!, null!);
    private readonly Mock<TwitchCommandHandler> _twitch = new(MockBehavior.Loose, null!);
    private readonly Mock<ClashCommandHandler> _clash = new(MockBehavior.Loose, null!, null!);

    private GeneralCommandHandler CreateHandler() => new(
        _reader.Object, _writer.Object,
        _text.Object, _spotify.Object, _obs.Object,
        _timer.Object, _twitch.Object, _clash.Object);

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
        _reader.Setup(r => r.ReadAfgeleidCounter()).Returns("5");

        string result = await CreateHandler().HandleCommand(Cmd("AFGELEID"), CancellationToken.None);

        Assert.Contains("6", result);
    }

    [Fact]
    public async Task HandleAfgeleid_IncrementsCounterAndReturnsMessage()
    {
        _reader.Setup(r => r.ReadAfgeleidCounter()).Returns("3");

        string result = await CreateHandler().HandleCommand(Cmd("afgeleid"), CancellationToken.None);

        _writer.Verify(w => w.WriteAfgeleidCounter("4"));
        Assert.Equal("Spekkie is 4x afgeleid geweest", result);
    }
}
