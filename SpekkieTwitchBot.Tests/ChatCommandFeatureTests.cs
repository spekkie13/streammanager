using Moq;
using SpekkieClassLibrary.Twitch;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.General.FileHandling;
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
    private readonly Mock<ICommandPermissionService> _Permissions = new();
    private readonly Mock<Logger> _Log = new(MockBehavior.Loose, null!);

    public ChatCommandFeatureTests()
    {
        _Permissions.Setup(p => p.IsAllowed(It.IsAny<string>(), It.IsAny<UserRole>())).Returns(true);
    }

    private ChatCommandFeature CreateFeature() => new(_Chat.Object, _Text.Object, _General.Object, _Permissions.Object, _Log.Object);

    private static ChatCommandReceived Cmd(string cmd, string args = "", UserRole role = UserRole.Viewer) =>
        new("mid", "uid", "user", role, cmd, args, $"!{cmd} {args}");

    [Fact]
    public async Task OnCommand_EmptyCommandText_DoesNothing()
    {
        await CreateFeature().OnCommandAsync(Cmd(""), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnCommand_CommandManagement_ViewerRole_DeniesAccess()
    {
        await CreateFeature().OnCommandAsync(Cmd("command", "add !hi hello", UserRole.Viewer), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("permission")), It.IsAny<CancellationToken>()));
        _Text.Verify(t => t.AddCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnCommand_CommandManagement_MissingArgs_SendsUsageMessage()
    {
        await CreateFeature().OnCommandAsync(Cmd("command", "", UserRole.Moderator), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("Usage:")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_CommandManagement_MissingCommandName_SendsUsageMessage()
    {
        await CreateFeature().OnCommandAsync(Cmd("command", "add", UserRole.Moderator), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("Usage:")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_CommandManagement_ValidArgs_CallsAddCommandAndReplies()
    {
        _Text.Setup(t => t.AddCommand("add", "!hi", "hello there")).Returns("Command !hi is added.");

        await CreateFeature().OnCommandAsync(Cmd("command", "add !hi hello there", UserRole.Moderator), CancellationToken.None);

        _Text.Verify(t => t.AddCommand("add", "!hi", "hello there"));
        _Chat.Verify(c => c.ReplyAsync("mid", "Command !hi is added.", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task OnCommand_InsufficientRole_RepliesWithPermissionDenied()
    {
        _Permissions.Setup(p => p.IsAllowed("!next", UserRole.Viewer)).Returns(false);

        await CreateFeature().OnCommandAsync(Cmd("next", "", UserRole.Viewer), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("permission")), It.IsAny<CancellationToken>()));
        _General.Verify(g => g.HandleCommand(It.IsAny<ChatCommandReceived>(), It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact]
    public async Task OnCommand_HandlerThrows_RepliesWithErrorMessage()
    {
        _Text.Setup(t => t.GetTextCommands()).Returns([]);
        _General.Setup(g => g.HandleCommand(It.IsAny<ChatCommandReceived>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("API failure"));

        await CreateFeature().OnCommandAsync(Cmd("uptime"), CancellationToken.None);

        _Chat.Verify(c => c.ReplyAsync("mid", It.Is<string>(s => s.Contains("went wrong")), It.IsAny<CancellationToken>()));
    }
}
