using System.Net;
using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;

public class FileBackedTwitchAuthTokenProvider : ITwitchAuthTokenProvider
{
    private readonly ITwitchFileReader _reader;
    private readonly ITwitchFileWriter _writer;
    private readonly HttpClient _httpClient;
    private readonly Logger _logger;
    
    private TwitchGeneralFile? _generalFileCache;
    private TwitchUserFile? _userFileCache;

    public FileBackedTwitchAuthTokenProvider(
        ITwitchFileReader reader,
        ITwitchFileWriter writer,
        HttpClient httpClient,
        Logger logger
    ) {
        _reader = reader;
        _writer = writer;
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<string> GetClientIdAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync();
        return auth.ClientId ?? "";
    }
    
    public async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        TwitchUserFile auth = await ReadTokensAsync();

        if (!string.IsNullOrWhiteSpace(auth.UserToken))
            return auth.UserToken;

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

        _logger.LogError("No usable Twitch token: missing access token and refresh token");
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
    
    private async Task<TwitchUserFile> RefreshUserAccessTokenAsync(TwitchUserFile userAuth, CancellationToken ct) {
        using FormUrlEncodedContent content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", userAuth.ClientId ?? ""),
            new KeyValuePair<string, string>("client_secret", userAuth.ClientSecret ?? ""),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", userAuth.UserRefreshToken ?? "")
        ]);

        HttpResponseMessage response = await _httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Error refreshing token: {(int)response.StatusCode} {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(payload) ?? AuthorizationCredentials.Empty;
        _logger.LogInfo($"Refreshed token successfully: token_type={cred.TokenType}, expires_in={cred.ExpiresIn}, scope={string.Join(" ", cred.Scope ?? [])}, access_token=***, refresh_token=***");

        userAuth.UserToken = cred.AccessToken;
        if (!string.IsNullOrWhiteSpace(cred.RefreshToken))
            userAuth.UserRefreshToken = cred.RefreshToken;

        return userAuth;
    }

    private async Task<TwitchUserFile> ExchangeCodeAsync(TwitchUserFile userAuth, CancellationToken ct) {
        using FormUrlEncodedContent content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", userAuth.ClientId ?? ""),
            new KeyValuePair<string, string>("client_secret", userAuth.ClientSecret ?? ""),
            new KeyValuePair<string, string>("code", userAuth.Code ?? ""),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        ]);

        HttpResponseMessage response = await _httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            if (!string.IsNullOrWhiteSpace(userAuth.UserRefreshToken))
                return await RefreshUserAccessTokenAsync(userAuth, ct);

            _logger.LogError("Code exchange failed (BadRequest) and no refresh token available.");
            return userAuth;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to exchange code. Status code: {response.StatusCode}");
            return userAuth;
        }

        string payload = await response.Content.ReadAsStringAsync(ct);
        AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(payload) ?? AuthorizationCredentials.Empty;
        _logger.LogInfo($"Code exchanged successfully: token_type={cred.TokenType}, expires_in={cred.ExpiresIn}, scope={string.Join(" ", cred.Scope ?? [])}, access_token=***, refresh_token=***");

        userAuth.UserToken = cred.AccessToken;
        userAuth.UserRefreshToken = cred.RefreshToken;
        
        userAuth.Code = null;

        return userAuth;
    }

    private Task PersistAuthAsync(TwitchUserFile userAuth)
    {
        _userFileCache = userAuth;
        string json = JsonConvert.SerializeObject(userAuth, Formatting.Indented);
        _writer.WriteTwitchUserAuthFile(json);
        return Task.CompletedTask;
    }
    
    public Task<TwitchGeneralFile> ReadIdentityAsync(CancellationToken cancellationToken)
    {
        if (_generalFileCache is not null) return Task.FromResult(_generalFileCache);

        string json = _reader.ReadTwitchGeneralAuthFile(); 
        _generalFileCache = JsonConvert.DeserializeObject<TwitchGeneralFile>(json) ?? new TwitchGeneralFile();
        return Task.FromResult(_generalFileCache);
    }

    private Task<TwitchUserFile> ReadTokensAsync()
    {
        if (_userFileCache is not null) return Task.FromResult(_userFileCache);

        string json = _reader.ReadTwitchUserAuthFile();
        _userFileCache = JsonConvert.DeserializeObject<TwitchUserFile>(json) ?? new TwitchUserFile();
        return Task.FromResult(_userFileCache);
    }
}