using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Routing;

public sealed class TwitchEventRouter
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchEvents _Events;
    private readonly ChatCommandFeature _ChatCommands;
    private readonly ChatMessageFeature _ChatMessages;
    private readonly FollowSubFeature _FollowSub;
    private readonly ChannelPointsFeature _ChannelPoints;
    
    public TwitchEventRouter(
        ITwitchChat chat,
        ITwitchEvents events,
        ChatCommandFeature chatCommands,
        ChatMessageFeature chatMessages,
        FollowSubFeature followSub,
        ChannelPointsFeature channelPoints    
    ) {
        _Chat = chat;
        _Events = events;
        _ChatCommands = chatCommands;
        _ChatMessages = chatMessages;
        _FollowSub = followSub;
        _ChannelPoints = channelPoints;
    }
    
    public void Wire()
    {
        _Chat.OnChatCommandReceived += OnChatCommandReceived;
        _Chat.OnChatMessageReceived += OnChatMessageReceived;

        _Events.OnFollow += OnFollow;
        _Events.OnSub += OnSub;
        _Events.OnChannelPointRedeemed += OnChannelPointRedeemed;
    }

    public void Unwire()
    {
        _Chat.OnChatCommandReceived -= OnChatCommandReceived;
        _Chat.OnChatMessageReceived -= OnChatMessageReceived;

        _Events.OnFollow -= OnFollow;
        _Events.OnSub -= OnSub;
        _Events.OnChannelPointRedeemed -= OnChannelPointRedeemed;
    }

    private Task OnChatCommandReceived(ChatCommandReceived ev)
        => _ChatCommands.OnCommandAsync(ev);

    private Task OnChatMessageReceived(ChatMessageReceived ev)
        => _ChatMessages.OnMessageAsync(ev);

    private Task OnFollow(FollowHappened ev, CancellationToken ct)
        => _FollowSub.HandleFollowAsync(ev, ct);

    private Task OnSub(SubHappened ev, CancellationToken ct)
        => _FollowSub.HandleSubAsync(ev, ct);

    private async Task OnChannelPointRedeemed(ChannelPointRedeemed ev, CancellationToken ct)
    {
        string message = await _ChannelPoints.OnRedeemedAsync(ev, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(message)) return;

        if (string.Equals(message, "Ignored", StringComparison.OrdinalIgnoreCase)) return;
        
        await _Chat.SendAsync(message, ct).ConfigureAwait(false);
    }
}