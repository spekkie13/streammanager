using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;

public interface ITwitchAuthTokenProvider
{
    Task<string> GetClientIdAsync(CancellationToken cancellationToken);
    Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken);
    Task<TwitchGeneralFile> ReadIdentityAsync(CancellationToken cancellationToken);
    
    Task ForceRefreshAsync(CancellationToken cancellationToken);
}