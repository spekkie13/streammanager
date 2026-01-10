using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public sealed class TwitchEventsFeature
{
    private readonly FollowSubFeature _FollowSub;

    public TwitchEventsFeature(FollowSubFeature followSub)
    {
        _FollowSub = followSub;
    }

    public Task OnSubAsync(SubHappened e, CancellationToken cancellationToken = default) =>
        _FollowSub.HandleSubAsync(e, cancellationToken);

    public Task OnFollowAsync(FollowHappened e, CancellationToken cancellationToken = default) =>
        _FollowSub.HandleFollowAsync(e, cancellationToken);
}