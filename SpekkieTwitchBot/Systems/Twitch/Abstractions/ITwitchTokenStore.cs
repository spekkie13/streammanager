using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchTokenStore
{
    Task<TwitchUserFile> LoadUserAsync(CancellationToken cancellationToken);
    Task SaveUserAsync(TwitchUserFile user, CancellationToken cancellationToken);

    Task<TwitchGeneralFile> LoadGeneralSettingsAsync(CancellationToken cancellationToken);
    Task SaveGeneralSettingsAsync(TwitchGeneralFile general, CancellationToken cancellationToken);
}