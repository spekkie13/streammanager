namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;

public interface ITwitchAuthTokenProvider
{
    Task<string> GetClientIdAsync(CancellationToken cancellationToken);
    Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken);
    Task<string> GetChannelIdAsync(CancellationToken cancellationToken);
    Task<string> GetBroadcasterNameAsync(CancellationToken cancellationToken);
    
    Task ForceRefreshAsync(CancellationToken cancellationToken);
}