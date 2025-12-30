using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Adapters;

public class PubSubEventsAdapter : ITwitchEvents
{
    public event Func<FollowHappened, CancellationToken, Task>? OnFollow;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;
    public event Func<ChannelPointRedeemed, CancellationToken, Task>? OnChannelPointRedeemed;
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}