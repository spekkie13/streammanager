namespace SpekkieTwitchBot.Systems.Twitch.Abstractions;

public interface ITwitchTokenProvider
{
    Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken);
    Task<string> GetChatOAuthAsync(CancellationToken cancellationToken);
}