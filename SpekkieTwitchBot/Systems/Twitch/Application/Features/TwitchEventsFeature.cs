using SpekkieTwitchBot.Systems.Twitch.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Features;

public sealed class TwitchEventsFeature
{
    private readonly FollowSubFeature _followSub;

    public Task OnSubAsync(SubHappened e, CancellationToken cancellationToken = default) =>
        _followSub.HandleSubAsync(e, cancellationToken);

    public Task OnFollowAsync(FollowHappened e, CancellationToken cancellationToken = default) =>
        _followSub.HandleFollowAsync(e, cancellationToken);
}