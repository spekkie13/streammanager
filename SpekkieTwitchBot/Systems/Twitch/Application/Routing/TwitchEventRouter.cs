using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Routing;

public sealed class TwitchEventRouter(
    ITwitchChat chat,
    ITwitchEvents events,
    ChatCommandFeature chatCommands,
    ChatMessageFeature chatMessages,
    ChatOverlayService chatOverlay,
    FollowSubFeature followSub,
    ChannelPointsFeature channelPoints,
    TimedMessagesFeature timedMessages,
    MarathonTimerFeature marathon,
    SupportTotalsFeature supportTotals)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await followSub.InitializeAsync(ct);
        await timedMessages.InitializeAsync(ct);
        await supportTotals.InitializeAsync(ct);
    }

    public void Wire()
    {
        chat.OnChatCommandReceived += OnChatCommandReceived;
        chat.OnChatMessageReceived += OnChatMessageReceived;

        events.OnFollow += OnFollow;
        events.OnSub += OnSub;
        events.OnBits += OnBits;
        events.OnChannelPointRedeemed += OnChannelPointRedeemed;
    }

    public void Unwire()
    {
        chat.OnChatCommandReceived -= OnChatCommandReceived;
        chat.OnChatMessageReceived -= OnChatMessageReceived;

        events.OnFollow -= OnFollow;
        events.OnSub -= OnSub;
        events.OnBits -= OnBits;
        events.OnChannelPointRedeemed -= OnChannelPointRedeemed;
    }

    private Task OnChatCommandReceived(ChatCommandReceived ev)
        => chatCommands.OnCommandAsync(ev);

    private Task OnChatMessageReceived(ChatMessageReceived ev)
    {
        chatOverlay.Append(ev);
        return chatMessages.OnMessageAsync(ev);
    }

    private Task OnFollow(FollowHappened ev, CancellationToken ct)
        => followSub.HandleFollowAsync(ev, ct);

    private Task OnSub(SubHappened ev, CancellationToken ct)
        => Task.WhenAll(
            followSub.HandleSubAsync(ev, ct),
            marathon.HandleSubAsync(ev, ct));

    private Task OnBits(BitsHappened ev, CancellationToken ct)
        => Task.WhenAll(
            marathon.HandleBitsAsync(ev, ct),
            supportTotals.HandleBitsAsync(ev, ct));

    private async Task OnChannelPointRedeemed(ChannelPointRedeemed ev, CancellationToken ct)
    {
        string message = await channelPoints.OnRedeemedAsync(ev, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(message)) return;

        if (string.Equals(message, "Ignored", StringComparison.OrdinalIgnoreCase)) return;
        
        await chat.SendAsync(message, ct).ConfigureAwait(false);
    }
}