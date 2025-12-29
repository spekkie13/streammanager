using CommandService.CommandHandlers;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Features;

public sealed class ChatCommandFeature
{
    private readonly ITwitchChat _Chat;
    private readonly TextCommandHandler _Text;
    private readonly GeneralCommandHandler _General;
    
    public ChatCommandFeature(
        ITwitchChat chat, 
        TextCommandHandler text, 
        GeneralCommandHandler general
    ) {
        _Chat = chat;
        _Text = text;
        _General = general;
    }

    public async Task OnCommandAsync(ChatCommandReceived e, CancellationToken cancellationToken = default)
    {
        var cmdText = (e.CommandText ?? "").Trim();
        if (cmdText.Length == 0) return;

        var command = "!" + cmdText.ToLowerInvariant();

        // 1) Special: !command
        if (string.Equals(cmdText, "command", StringComparison.OrdinalIgnoreCase))
        {
            var parts = (e.ArgumentsAsString ?? "")
                .Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

            var action = parts.ElementAtOrDefault(0) ?? "";
            var commandName = parts.ElementAtOrDefault(1) ?? "";
            var replyMessage = parts.Length > 2 ? parts[2] : "";

            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(commandName))
            {
                await _Chat.ReplyAsync(
                    e.MessageId,
                    "Usage: !command <add|remove|edit> <name> <reply>",
                    cancellationToken
                );
                return;
            }

            var adminReply = _Text.AddCommand(action, commandName, replyMessage);
            if (!string.IsNullOrWhiteSpace(adminReply))
                await _Chat.ReplyAsync(e.MessageId, adminReply, cancellationToken);

            return;
        }

        var commands = _Text.GetTextCommands();
        var reply = commands.Any(x => string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase))
            ? _Text.HandleCommand(e)
            : _General.HandleCommand(e);

        if (!string.IsNullOrWhiteSpace(reply))
            await _Chat.ReplyAsync(e.MessageId, reply, cancellationToken);
    }
}