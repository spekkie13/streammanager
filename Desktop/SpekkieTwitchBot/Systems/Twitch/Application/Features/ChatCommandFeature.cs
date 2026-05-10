using SpekkieClassLibrary.Twitch;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatCommandFeature
{
    private readonly ITwitchChat _Chat;
    private readonly ITextCommandHandler _Text;
    private readonly IGeneralCommandHandler _General;
    private readonly ICommandPermissionService _Permissions;
    private readonly Logger _Log;

    public ChatCommandFeature(
        ITwitchChat chat,
        ITextCommandHandler text,
        IGeneralCommandHandler general,
        ICommandPermissionService permissions,
        Logger log
    ) {
        _Chat = chat;
        _Text = text;
        _General = general;
        _Permissions = permissions;
        _Log = log;
    }

    public async Task OnCommandAsync(ChatCommandReceived e, CancellationToken cancellationToken = default)
    {
        string cmdText = (e.CommandText ?? "").Trim();
        if (cmdText.Length == 0) return;

        string command = "!" + cmdText.ToLowerInvariant();

        if (string.Equals(cmdText, "command", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Role < UserRole.Moderator)
            {
                await _Chat.ReplyAsync(e.MessageId, "You don't have permission to manage commands.", cancellationToken);
                return;
            }

            string[] parts = (e.ArgumentsAsString ?? "")
                .Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

            string action = parts.ElementAtOrDefault(0) ?? "";
            string commandName = parts.ElementAtOrDefault(1) ?? "";
            string replyMessage = parts.Length > 2 ? parts[2] : "";

            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(commandName))
            {
                await _Chat.ReplyAsync(
                    e.MessageId,
                    "Usage: !command <add|remove|edit> <name> <reply>",
                    cancellationToken
                );
                return;
            }

            string adminReply = _Text.AddCommand(action, commandName, replyMessage);
            if (!string.IsNullOrWhiteSpace(adminReply))
                await _Chat.ReplyAsync(e.MessageId, adminReply, cancellationToken);

            return;
        }

        if (!_Permissions.IsAllowed(command, e.Role))
        {
            await _Chat.ReplyAsync(e.MessageId, $"You don't have permission to use {command}.", cancellationToken);
            return;
        }

        try
        {
            List<TextCommand> commands = _Text.GetTextCommands();
            string reply = commands.Any(x => string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase))
                ? _Text.HandleCommand(e)
                : await _General.HandleCommand(e, cancellationToken);

            await _Chat.ReplyAsync(e.MessageId, reply, cancellationToken);
        }
        catch (Exception ex)
        {
            _Log.LogError($"[Command] {command} threw: {ex.Message}");
            await _Chat.ReplyAsync(e.MessageId, $"Something went wrong executing {command}.", cancellationToken);
        }
    }
}