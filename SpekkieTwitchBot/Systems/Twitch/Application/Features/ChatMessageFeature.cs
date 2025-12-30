using CommandService.CommandHandlers;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;

namespace SpekkieTwitchBot.Systems.Twitch.Features;

public sealed class ChatMessageFeature
{
    private readonly ITwitchChat _Chat;
    private readonly ChannelPointsFeature _ChannelPoints;
    private readonly SpotifyCommandHandler _Spotify;

    public async Task OnMessageAsync(ChatMessageReceived e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(e.CustomRewardId)) return;

        var redemptions = await _ChannelPoints.GetCustomRedemptions();
        var redemption = redemptions.FirstOrDefault(r => r.Id == e.CustomRewardId);
        if (redemption == null) return;

        var reply = redemption.Title switch
        {
            "Song request" => _Spotify.HandleAddSongToQueueCommand(e.Text),
            "Hydrate" => "Take a Sip Spekkie!",
            _ => ""
        };
        
        if (!string.IsNullOrEmpty(reply))
            await _Chat.ReplyAsync(e.MessageId, reply, cancellationToken);
    }
}