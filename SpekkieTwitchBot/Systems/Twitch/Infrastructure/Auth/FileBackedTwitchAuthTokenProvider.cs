using System.Net;
using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;

public class FileBackedTwitchAuthTokenProvider(
    TwitchFileReader reader,
    TwitchFileWriter writer,
    HttpClient httpClient,
    Logger logger
) : ITwitchAuthTokenProvider
{
    private TwitchGeneralFile? _GeneralFileCache;
    private TwitchUserFile? _UserFileCache;
    
    public async Task<string> GetClientIdAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync();
        return auth.ClientId ?? "";
    }

    public async Task<string> GetChannelIdAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile identity = await ReadIdentityAsync();
        return identity.ChannelId ?? "";
    }
    
    public async Task<string> GetBroadcasterNameAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile identity = await ReadIdentityAsync();
        return identity.BroadcasterName ?? "";
    }
    
    public async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync();

        if (!string.IsNullOrWhiteSpace(auth.UserRefreshToken))
        {
            TwitchUserFile refreshed = await RefreshUserAccessTokenAsync(auth, cancellationToken);
            await PersistAuthAsync(refreshed);
            return refreshed.UserToken ?? "";
        }

        if (!string.IsNullOrWhiteSpace(auth.Code))
        {
            TwitchUserFile exchanged = await ExchangeCodeAsync(auth, cancellationToken);
            await PersistAuthAsync(exchanged);
            return exchanged.UserToken ?? "";
        }

        logger.LogError("No usable Twitch token: missing access token and refresh token");
        return "";
    }

    public async Task ForceRefreshAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync();

        if (string.IsNullOrWhiteSpace(auth.UserRefreshToken))
            throw new InvalidOperationException(
                "Cannot force refresh: no refresh token available.");

        TwitchUserFile refreshed =
            await RefreshUserAccessTokenAsync(auth, cancellationToken);

        await PersistAuthAsync(refreshed);
    }
    
    private async Task<TwitchUserFile> RefreshUserAccessTokenAsync(
        TwitchUserFile userAuth,
        CancellationToken ct
    ) {
        using FormUrlEncodedContent content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", userAuth.ClientId ?? ""),
            new KeyValuePair<string, string>("client_secret", userAuth.ClientSecret ?? ""),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", userAuth.UserRefreshToken ?? "")
        ]);

        HttpResponseMessage response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Error refreshing token: {(int)response.StatusCode} {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        logger.LogInfo($"Refreshed token successfully: {payload}");

        AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(payload) ?? AuthorizationCredentials.Empty;

        userAuth.UserToken = cred.AccessToken;
        if (!string.IsNullOrWhiteSpace(cred.RefreshToken))
            userAuth.UserRefreshToken = cred.RefreshToken;

        return userAuth;
    }

    private async Task<TwitchUserFile> ExchangeCodeAsync(
        TwitchUserFile userAuth,
        CancellationToken ct
    ) {
        using FormUrlEncodedContent content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", userAuth.ClientId ?? ""),
            new KeyValuePair<string, string>("client_secret", userAuth.ClientSecret ?? ""),
            new KeyValuePair<string, string>("code", userAuth.Code ?? ""),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        ]);

        HttpResponseMessage response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            if (!string.IsNullOrWhiteSpace(userAuth.UserRefreshToken))
                return await RefreshUserAccessTokenAsync(userAuth, ct);

            logger.LogError("Code exchange failed (BadRequest) and no refresh token available.");
            return userAuth;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to exchange code. Status code: {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        logger.LogInfo(payload);

        AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(payload) ?? AuthorizationCredentials.Empty;

        userAuth.UserToken = cred.AccessToken;
        userAuth.UserRefreshToken = cred.RefreshToken;
        
        userAuth.Code = null;

        return userAuth;
    }

    private Task PersistAuthAsync(TwitchUserFile userAuth)
    {
        _UserFileCache = userAuth;
        string json = JsonConvert.SerializeObject(userAuth, Formatting.Indented);
        writer.WriteTwitchUserAuthFile(json);
        return Task.CompletedTask;
    }
    
    private Task<TwitchGeneralFile> ReadIdentityAsync()
    {
        if (_GeneralFileCache is not null) return Task.FromResult(_GeneralFileCache);

        string json = reader.ReadTwitchGeneralAuthFile(); 
        _GeneralFileCache = JsonConvert.DeserializeObject<TwitchGeneralFile>(json) ?? new TwitchGeneralFile();
        return Task.FromResult(_GeneralFileCache);
    }

    private Task<TwitchUserFile> ReadTokensAsync()
    {
        if (_UserFileCache is not null) return Task.FromResult(_UserFileCache);

        string json = reader.ReadTwitchUserAuthFile();
        _UserFileCache = JsonConvert.DeserializeObject<TwitchUserFile>(json) ?? new TwitchUserFile();
        return Task.FromResult(_UserFileCache);
    }
}