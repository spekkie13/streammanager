using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatMessageFeature(ITwitchChat chat, ITwitchChannelInfoClient channelInfo)
{
    private readonly HashSet<string> _SeenThisStream = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private string? _CurrentStreamId;
    private DateTime _LastStreamIdFetch = DateTime.MinValue;
    private static readonly TimeSpan StreamIdCacheDuration = TimeSpan.FromMinutes(5);

    public async Task OnMessageAsync(ChatMessageReceived e, CancellationToken cancellationToken = default)
    {
        if (e.Text.StartsWith('!')) return;

        if (DateTime.UtcNow - _LastStreamIdFetch >= StreamIdCacheDuration)
        {
            string? streamId = await channelInfo.GetCurrentStreamIdAsync(cancellationToken);
            _LastStreamIdFetch = DateTime.UtcNow;
            if (streamId != _CurrentStreamId)
            {
                _CurrentStreamId = streamId;
                _SeenThisStream.Clear();
            }
        }

        if (!_SeenThisStream.Add(e.UserId)) return;

        await chat.ReplyAsync(e.MessageId, $"hi {e.Username}!", cancellationToken);
    }
}