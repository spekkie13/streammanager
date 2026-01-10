using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

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
        string cmdText = (e.CommandText ?? "").Trim();
        if (cmdText.Length == 0) return;

        string command = "!" + cmdText.ToLowerInvariant();

        if (string.Equals(cmdText, "command", StringComparison.OrdinalIgnoreCase))
        {
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

        List<TextCommand> commands = _Text.GetTextCommands();
        string reply = commands.Any(x => string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase))
            ? _Text.HandleCommand(e)
            : await _General.HandleCommand(e, cancellationToken);

        await _Chat.ReplyAsync(e.MessageId, reply, cancellationToken);
    }
}