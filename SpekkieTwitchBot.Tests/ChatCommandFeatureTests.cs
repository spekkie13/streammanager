using Moq;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Tests;

public class ChatCommandFeatureTests
{
    private readonly Mock<ITwitchChat> _Chat = new();
    private readonly Mock<ITextCommandHandler> _Text = new();
    private readonly Mock<IGeneralCommandHandler> _General = new();

    private ChatCommandFeature CreateFeature() => new(_Chat.Object, _Text.Object, _General.Object);

    private static ChatCommandReceived Cmd(string cmd, string args = "") =>
        new("mid", "uid", "user", cmd, args, $"!{cmd} {args}");

    [Fact]
    public async Task OnCommand_EmptyCommandText_DoesNothing()
    {
        await CreateFeature().OnCommandAsync(Cmd(""), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnCommand_CommandManagement_MissingArgs_SendsUsageMessage()
    {
        await CreateFeature().OnCommandAsync(Cmd("command"), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("Usage:")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_CommandManagement_MissingCommandName_SendsUsageMessage()
    {
        await CreateFeature().OnCommandAsync(Cmd("command", "add"), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("Usage:")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_CommandManagement_ValidArgs_CallsAddCommandAndReplies()
    {
        _Text.Setup(t => t.AddCommand("add", "!hi", "hello there")).Returns("Command !hi is added.");

        await CreateFeature().OnCommandAsync(Cmd("command", "add !hi hello there"), CancellationToken.None);

        _Text.Verify(t => t.AddCommand("add", "!hi", "hello there"));
        _Chat.Verify(c => c.ReplyAsync("mid", "Command !hi is added.", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_KnownTextCommand_UsesTextHandler()
    {
        _Text.Setup(t => t.GetTextCommands())
             .Returns([new TextCommand { Command = "!hi", Response = "hello" }]);
        _Text.Setup(t => t.HandleCommand(It.IsAny<ChatCommandReceived>())).Returns("hello");

        await CreateFeature().OnCommandAsync(Cmd("hi"), CancellationToken.None);

        _Text.Verify(t => t.HandleCommand(It.IsAny<ChatCommandReceived>()));
        _General.Verify(g => g.HandleCommand(It.IsAny<ChatCommandReceived>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnCommand_UnknownTextCommand_FallsBackToGeneralHandler()
    {
        _Text.Setup(t => t.GetTextCommands()).Returns([]);
        _General.Setup(g => g.HandleCommand(It.IsAny<ChatCommandReceived>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("general reply");

        await CreateFeature().OnCommandAsync(Cmd("song"), CancellationToken.None);

        _General.Verify(g => g.HandleCommand(It.IsAny<ChatCommandReceived>(), It.IsAny<CancellationToken>()));
        _Chat.Verify(c => c.ReplyAsync("mid", "general reply", It.IsAny<CancellationToken>()));
    }
}
