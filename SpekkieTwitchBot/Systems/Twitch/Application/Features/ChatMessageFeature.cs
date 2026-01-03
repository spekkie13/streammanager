using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class ChatMessageFeature
{
    private readonly ITwitchChat _Chat;
    private readonly ChannelPointsFeature _ChannelPoints;
    private readonly SpotifyCommandHandler _Spotify;

    public async Task OnMessageAsync(ChatMessageReceived e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(e.CustomRewardId)) return;
        
        List<ChannelPointData> redemptions = await _ChannelPoints.GetCustomRedemptions();
        ChannelPointData? redemption = redemptions.FirstOrDefault(r => r.Id == e.CustomRewardId);
        if (redemption == null) return;

        string reply = redemption.Title switch
        {
            "Song request" => _Spotify.HandleAddSongToQueueCommand(e.Text),
            "Hydrate" => "Take a Sip Spekkie!",
            _ => ""
        };
        
        if (!string.IsNullOrEmpty(reply))
            await _Chat.ReplyAsync(e.MessageId, reply, cancellationToken);
    }
}