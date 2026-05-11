using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchEvents
{
    event Func<FollowHappened, CancellationToken, Task> OnFollow;
    event Func<SubHappened, CancellationToken, Task> OnSub;
    event Func<BitsHappened, CancellationToken, Task> OnBits;
    event Func<ChannelPointRedeemed, CancellationToken, Task> OnChannelPointRedeemed;
    
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}