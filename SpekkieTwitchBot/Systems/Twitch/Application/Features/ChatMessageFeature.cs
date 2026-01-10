using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatMessageFeature
{
    private readonly ITwitchChat _Chat;

    public ChatMessageFeature(
        ITwitchChat chat
    ) {
        _Chat = chat;
    }

    public async Task OnMessageAsync(ChatMessageReceived e, CancellationToken cancellationToken = default)
    {
        if (e.Text.StartsWith('!')) return;
        
        const string Reply = "hi";
        
        if (!string.IsNullOrEmpty(Reply))
            await _Chat.ReplyAsync(e.MessageId, Reply, cancellationToken);
    }
}