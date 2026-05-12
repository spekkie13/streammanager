using SpekkieClassLibrary.Twitch;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatCommandFeature(
    ITwitchChat chat,
    ITextCommandHandler text,
    IGeneralCommandHandler general,
    ICommandPermissionService permissions,
    Logger log)
{
    public async Task OnCommandAsync(ChatCommandReceived e, CancellationToken cancellationToken = default)
    {
        string cmdText = (e.CommandText ?? "").Trim();
        if (cmdText.Length == 0) return;

        string command = "!" + cmdText.ToLowerInvariant();

        if (string.Equals(cmdText, "command", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Role < UserRole.Moderator)
            {
                await chat.ReplyAsync(e.MessageId, "You don't have permission to manage commands.", cancellationToken);
                return;
            }

            string[] parts = (e.ArgumentsAsString ?? "")
                .Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

            string action = parts.ElementAtOrDefault(0) ?? "";
            string commandName = parts.ElementAtOrDefault(1) ?? "";
            string replyMessage = parts.Length > 2 ? parts[2] : "";

            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(commandName))
            {
                await chat.ReplyAsync(
                    e.MessageId,
                    "Usage: !command <add|remove|edit> <name> <reply>",
                    cancellationToken
                );
                return;
            }

            string adminReply = text.AddCommand(action, commandName, replyMessage);
            if (!string.IsNullOrWhiteSpace(adminReply))
                await chat.ReplyAsync(e.MessageId, adminReply, cancellationToken);

            return;
        }

        if (!permissions.IsAllowed(command, e.Role))
        {
            await chat.ReplyAsync(e.MessageId, $"You don't have permission to use {command}.", cancellationToken);
            return;
        }

        try
        {
            List<TextCommand> commands = text.GetTextCommands();
            string reply = commands.Any(x => string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase))
                ? text.HandleCommand(e)
                : await general.HandleCommand(e, cancellationToken);

            await chat.ReplyAsync(e.MessageId, reply, cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError($"[Command] {command} threw: {ex.Message}");
            await chat.ReplyAsync(e.MessageId, $"Something went wrong executing {command}.", cancellationToken);
        }
    }
}