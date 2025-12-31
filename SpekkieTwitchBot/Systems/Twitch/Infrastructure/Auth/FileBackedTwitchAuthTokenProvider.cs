using System.Net;
using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;

public class FileBackedTwitchAuthTokenProvider : ITwitchAuthTokenProvider
{
    private readonly TwitchFileReader _Reader;
    private readonly TwitchFileWriter _Writer;
    private readonly HttpClient _HttpClient;
    private readonly Logger _Logger;

    private TwitchGeneralFile? _GeneralFileCache;
    private TwitchUserFile? _UserFileCache;

    public FileBackedTwitchAuthTokenProvider(
        TwitchFileReader reader,
        TwitchFileWriter writer,
        HttpClient httpClient,
        Logger logger
    )
    {
        _Reader = reader;
        _Writer = writer;
        _HttpClient = httpClient;
        _Logger = logger;
    }
    
    public async Task<string> GetClientIdAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync(cancellationToken);
        return auth.ClientId ?? "";
    }

    public async Task<string> GetChannelIdAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile identity = await ReadIdentityAsync(cancellationToken);
        return identity.ChannelId ?? "";
    }
    
    public async Task<string> GetBroadcasterNameAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile identity = await ReadIdentityAsync(cancellationToken);
        return identity.BroadcasterName ?? "";
    }
    
    public async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(auth.UserRefreshToken))
        {
            TwitchUserFile refreshed = await RefreshUserAccessTokenAsync(auth, cancellationToken);
            await PersistAuthAsync(refreshed, cancellationToken);
            return refreshed.UserToken ?? "";
        }

        if (!string.IsNullOrWhiteSpace(auth.Code))
        {
            TwitchUserFile exchanged = await ExchangeCodeAsync(auth, cancellationToken);
            await PersistAuthAsync(exchanged, cancellationToken);
            return exchanged.UserToken ?? "";
        }

        _Logger.LogError("No usable Twitch token: missing access token and refresh token");
        return "";
    }

    public async Task ForceRefreshAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.UserRefreshToken))
            throw new InvalidOperationException(
                "Cannot force refresh: no refresh token available.");

        TwitchUserFile refreshed =
            await RefreshUserAccessTokenAsync(auth, cancellationToken);

        await PersistAuthAsync(refreshed, cancellationToken);
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

        HttpResponseMessage response = await _HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            _Logger.LogError($"Error refreshing token: {(int)response.StatusCode} {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        _Logger.LogInfo($"Refreshed token successfully: {payload}");

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

        HttpResponseMessage response = await _HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            if (!string.IsNullOrWhiteSpace(userAuth.UserRefreshToken))
                return await RefreshUserAccessTokenAsync(userAuth, ct);

            _Logger.LogError("Code exchange failed (BadRequest) and no refresh token available.");
            return userAuth;
        }

        if (!response.IsSuccessStatusCode)
        {
            _Logger.LogError($"Failed to exchange code. Status code: {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        _Logger.LogInfo(payload);

        AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(payload) ?? AuthorizationCredentials.Empty;

        userAuth.UserToken = cred.AccessToken;
        userAuth.UserRefreshToken = cred.RefreshToken;
        
        userAuth.Code = null;

        return userAuth;
    }

    private Task PersistAuthAsync(TwitchUserFile userAuth, CancellationToken ct)
    {
        _UserFileCache = userAuth;
        string json = JsonConvert.SerializeObject(userAuth, Formatting.Indented);
        _Writer.WriteTwitchUserAuthFile(json);
        return Task.CompletedTask;
    }
    
    private Task<TwitchGeneralFile> ReadIdentityAsync(CancellationToken ct)
    {
        if (_GeneralFileCache is not null) return Task.FromResult(_GeneralFileCache);

        string json = _Reader.ReadTwitchGeneralAuthFile(); 
        _GeneralFileCache = JsonConvert.DeserializeObject<TwitchGeneralFile>(json) ?? new TwitchGeneralFile();
        return Task.FromResult(_GeneralFileCache);
    }

    private Task<TwitchUserFile> ReadTokensAsync(CancellationToken ct)
    {
        if (_UserFileCache is not null) return Task.FromResult(_UserFileCache);

        string json = _Reader.ReadTwitchUserAuthFile();
        _UserFileCache = JsonConvert.DeserializeObject<TwitchUserFile>(json) ?? new TwitchUserFile();
        return Task.FromResult(_UserFileCache);
    }
}