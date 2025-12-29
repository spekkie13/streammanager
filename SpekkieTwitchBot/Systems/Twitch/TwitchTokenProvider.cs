using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchTokenProvider : ITwitchTokenProvider
{
    private readonly ITwitchTokenStore _Store;
    private readonly TwitchOAuthClient _Oauth;
    private readonly TwitchUserFile _User;
    private readonly SemaphoreSlim _Lock = new(1, 1);

    public TwitchTokenProvider(
        ITwitchTokenStore store,
        TwitchOAuthClient oauth,
        TwitchUserFile user
    ) {
        _Store = store;
        _Oauth = oauth;
        _User = user;
    }
    
    public async Task<string> GetUserAccessTokenAsync(CancellationToken ct)
    {
        await _Lock.WaitAsync(ct);
        try
        {
            var tokens = await _Store.LoadUserAsync(ct);

            var cred = await _Oauth.RefreshUserTokenAsync(
                _User.ClientId,
                _User.ClientSecret,
                tokens.UserRefreshToken,
                ct);

            tokens.UserToken = cred.AccessToken!;
            tokens.UserRefreshToken = cred.RefreshToken!;

            await _Store.SaveUserAsync(tokens, ct);
            return tokens.UserToken;
        }
        finally
        {
            _Lock.Release();
        }
    }

    public async Task<string> GetChatOAuthAsync(CancellationToken ct)
        => $"oauth:{await GetUserAccessTokenAsync(ct)}";
}