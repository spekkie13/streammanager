using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Routing;

public class TwitchEventRouter(
    ITwitchChat chat,
    ITwitchEvents events,
    ChatCommandFeature chatCommands,
    FollowSubFeature followSub,
    ChannelPointsFeature channelPoints,
    CancellationToken ct) 
{
    public void Wire()
    {
        chat.OnChatCommandReceived += OnChatCommandReceived;

        events.OnFollow += OnFollow;
        events.OnSub += OnSub;
        events.OnChannelPointRedeemed += OnChannelPointRedeemed;
    }

    public void Unwire()
    {
        chat.OnChatCommandReceived -= OnChatCommandReceived;

        events.OnFollow -= OnFollow;
        events.OnSub -= OnSub;
        events.OnChannelPointRedeemed -= OnChannelPointRedeemed;
    }
    
    private Task OnChatCommandReceived(ChatCommandReceived ev)
        => chatCommands.OnCommandAsync(ev, ct);

    private Task OnFollow(FollowHappened ev, CancellationToken _)
        => followSub.HandleFollowAsync(ev, ct);

    private Task OnSub(SubHappened ev, CancellationToken _)
        => followSub.HandleSubAsync(ev, ct);

    private Task OnChannelPointRedeemed(ChannelPointRedeemed ev, CancellationToken _)
        => channelPoints.OnRedeemedAsync(ev, ct);
}