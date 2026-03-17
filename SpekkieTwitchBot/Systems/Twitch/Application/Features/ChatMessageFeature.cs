using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatMessageFeature(ITwitchChat chat)
{
    private readonly HashSet<string> _SeenToday = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private DateOnly _SeenDate = DateOnly.MinValue;

    public async Task OnMessageAsync(ChatMessageReceived e, CancellationToken cancellationToken = default)
    {
        if (e.Text.StartsWith('!')) return;

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (_SeenDate != today)
        {
            _SeenDate = today;
            _SeenToday.Clear();
        }

        if (!_SeenToday.Add(e.UserId)) return;

        string reply = $"hi {e.Username}!";

        if (!string.IsNullOrEmpty(reply))
            await chat.ReplyAsync(e.MessageId, reply, cancellationToken);
    }
}