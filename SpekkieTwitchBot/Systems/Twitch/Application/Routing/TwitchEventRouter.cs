using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Features;
using SpekkieTwitchBot.Systems.Twitch.Models;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchEventRouter
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchEvents _Events;

    private readonly ChatCommandFeature _ChatCommands;
    private readonly FollowSubFeature _FollowSub;
    private readonly ChannelPointsFeature _ChannelPoints;

    private CancellationToken _Ct;

    public TwitchEventRouter(
        ITwitchChat chat,
        ITwitchEvents events,
        ChatCommandFeature chatCommands,
        FollowSubFeature followSub,
        ChannelPointsFeature channelPoints
    )
    {
        _Chat = chat;
        _Events = events;
        _ChatCommands = chatCommands;
        _FollowSub = followSub;
        _ChannelPoints = channelPoints;
    }

    public void Wire(CancellationToken ct)
    {
        _Ct = ct;

        _Chat.OnChatCommandReceived += OnChatCommandReceived;

        _Events.OnFollow += OnFollow;
        _Events.OnSub += OnSub;
        _Events.OnChannelPointRedeemed += OnChannelPointRedeemed;
    }

    public void Unwire()
    {
        _Chat.OnChatCommandReceived -= OnChatCommandReceived;

        _Events.OnFollow -= OnFollow;
        _Events.OnSub -= OnSub;
        _Events.OnChannelPointRedeemed -= OnChannelPointRedeemed;
    }
    
    private Task OnChatCommandReceived(ChatCommandReceived ev)
        => _ChatCommands.OnCommandAsync(ev, _Ct);

    private Task OnFollow(FollowHappened ev, CancellationToken _)
        => _FollowSub.HandleFollowAsync(ev, _Ct);

    private Task OnSub(SubHappened ev, CancellationToken _)
        => _FollowSub.HandleSubAsync(ev, _Ct);

    private Task OnChannelPointRedeemed(ChannelPointRedeemed ev, CancellationToken _)
        => _ChannelPoints.OnRedeemedAsync(ev, _Ct);
}