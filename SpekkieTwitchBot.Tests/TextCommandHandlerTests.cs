using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Tests;

public class TextCommandHandlerTests : IDisposable
{
    private readonly string _FilePath;

    public TextCommandHandlerTests()
    {
        _FilePath = Path.GetTempFileName();
        File.WriteAllText(_FilePath, "{\"TextCommands\":[]}");
    }

    public void Dispose() => File.Delete(_FilePath);

    private TextCommandHandler CreateHandler() => new(_FilePath);

    private static ChatCommandReceived Cmd(string command) =>
        new("mid", "uid", "user", UserRole.Viewer, command, "", $"!{command}");

    // ── Initial state ────────────────────────────────────────────────────────

    [Fact]
    public void GetTextCommands_EmptyFile_ReturnsEmptyList()
    {
        Assert.Empty(CreateHandler().GetTextCommands());
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void AddCommand_Add_CommandAppearsInList()
    {
        TextCommandHandler handler = CreateHandler();

        handler.AddCommand("add", "!test", "hello world");

        Assert.Single(handler.GetTextCommands(), c => c.Command == "!test");
    }

    [Fact]
    public void AddCommand_Add_ReturnsConfirmation()
    {
        string result = CreateHandler().AddCommand("add", "!test", "hello");

        Assert.Contains("added", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddCommand_Add_PersistedAcrossInstances()
    {
        CreateHandler().AddCommand("add", "!test", "hello world");

        TextCommandHandler reloaded = CreateHandler();
        Assert.Single(reloaded.GetTextCommands(), c => c.Command == "!test" && c.Response == "hello world");
    }

    // ── Edit / Update ────────────────────────────────────────────────────────

    [Fact]
    public void AddCommand_Edit_UpdatesResponse()
    {
        TextCommandHandler handler = CreateHandler();
        handler.AddCommand("add", "!test", "original");

        handler.AddCommand("edit", "!test", "updated");

        Assert.Single(handler.GetTextCommands(), c => c.Command == "!test" && c.Response == "updated");
    }

    [Fact]
    public void AddCommand_Update_AlsoUpdatesResponse()
    {
        TextCommandHandler handler = CreateHandler();
        handler.AddCommand("add", "!test", "original");

        handler.AddCommand("update", "!test", "updated");

        Assert.Single(handler.GetTextCommands(), c => c.Response == "updated");
    }

    // ── Remove / Delete ──────────────────────────────────────────────────────

    [Fact]
    public void AddCommand_Remove_CommandDisappearsFromList()
    {
        TextCommandHandler handler = CreateHandler();
        handler.AddCommand("add", "!test", "hello");

        handler.AddCommand("remove", "!test", "");

        Assert.Empty(handler.GetTextCommands());
    }

    [Fact]
    public void AddCommand_Delete_CommandDisappearsFromList()
    {
        TextCommandHandler handler = CreateHandler();
        handler.AddCommand("add", "!test", "hello");

        handler.AddCommand("delete", "!test", "");

        Assert.Empty(handler.GetTextCommands());
    }

    // ── HandleCommand ────────────────────────────────────────────────────────

    [Fact]
    public void HandleCommand_KnownCommand_ReturnsResponse()
    {
        TextCommandHandler handler = CreateHandler();
        handler.AddCommand("add", "!socials", "Follow me on Twitter!");

        string result = handler.HandleCommand(Cmd("socials"));

        Assert.Equal("Follow me on Twitter!", result);
    }

    [Fact]
    public void HandleCommand_UnknownCommand_ReturnsUnknown()
    {
        string result = CreateHandler().HandleCommand(Cmd("doesnotexist"));

        Assert.Equal("Unknown Command", result);
    }

    // ── Case insensitivity of action ─────────────────────────────────────────

    [Fact]
    public void AddCommand_ActionIsCaseInsensitive()
    {
        TextCommandHandler handler = CreateHandler();

        handler.AddCommand("ADD", "!test", "hello");

        Assert.Single(handler.GetTextCommands());
    }
}
