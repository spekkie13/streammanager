using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchChat
{
    event Func<ChatCommandReceived, Task> OnChatCommandReceived;
    event Func<ChatMessageReceived, Task> OnChatMessageReceived;
    
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
    
    Task ReplyAsync(string replyToMessageId, string message, CancellationToken cancellationToken);
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}