using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Spotify;

namespace SpotifyAuthService.Auth;

public class SpotifyAuthService
{
    private readonly SpotifyFileReader _SpotifyFileReader;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private readonly Logger _Logger;
    // Null in production (nothing registers an HttpMessageHandler, so DI uses the default); tests
    // supply a stub so the token endpoint can be exercised without touching the network.
    private readonly HttpMessageHandler? _HttpMessageHandler;
    private static SpotifyAuth? _SpotifyAuth;
    // The auth file is re-read on every (re)configure; only log when its contents actually changed so a
    // reconnect loop can't fill the log with identical "Spotify Auth loaded" lines.
    private static string? _LastLoggedAuth;

    public SpotifyAuthService(
        SpotifyFileReader spotifyFileReader,
        SpotifyFileWriter spotifyFileWriter,
        Logger logger,
        HttpMessageHandler? httpMessageHandler = null)
    {
        _SpotifyFileReader = spotifyFileReader;
        _SpotifyFileWriter = spotifyFileWriter;
        _Logger = logger;
        _HttpMessageHandler = httpMessageHandler;
    }

    // When this changes, an out-of-band re-auth has happened and a failed grant is worth retrying.
    public virtual DateTime GetAuthFileStampUtc() => _SpotifyFileReader.GetSpotifyAuthLastWriteUtc();

    public virtual SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = _SpotifyFileReader.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();

        string fingerprint = $"{_SpotifyAuth.ClientId}|{_SpotifyAuth.RefreshToken}";
        if (fingerprint != _LastLoggedAuth)
        {
            _LastLoggedAuth = fingerprint;
            _Logger.LogInfo($"Spotify Auth loaded: client_id={_SpotifyAuth.ClientId}, client_secret=***, token=***, refresh_token=***");
        }

        return _SpotifyAuth;
    }
    
    public virtual async Task<SpotifyAuth> FixAuth(SpotifyAuth spotifyAuth)
    {
        TokenResponse tokenData = await RefreshAccessTokenAsync(spotifyAuth);
        spotifyAuth.Token = tokenData.AccessToken;

        // Spotify may hand back a rotated refresh_token. Persisting it is what keeps the next
        // refresh working — dropping it leaves the file holding a superseded token.
        if (!string.IsNullOrWhiteSpace(tokenData.RefreshToken) &&
            tokenData.RefreshToken != spotifyAuth.RefreshToken)
        {
            spotifyAuth.RefreshToken = tokenData.RefreshToken;
            PersistAuth(spotifyAuth);
        }

        return spotifyAuth;
    }

    private void PersistAuth(SpotifyAuth spotifyAuth)
    {
        try
        {
            // Ignore nulls so round-tripping the file never introduces keys it did not have.
            string json = JsonConvert.SerializeObject(spotifyAuth, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            _SpotifyFileWriter.WriteSpotifyAuthFile(json);
            _LastLoggedAuth = null; // contents changed — let the next load log the new fingerprint
            _Logger.LogInfo("[SPOTIFY] Rotated refresh_token persisted to Spotify.json");
        }
        catch (Exception ex)
        {
            // A failed write is not fatal for this session — the in-memory token still works until
            // it expires — but it will resurface as invalid_grant later, so make it visible now.
            _Logger.LogError($"[SPOTIFY] Could not persist rotated refresh_token: {ex.Message}");
        }
    }
    
    /*
    public async Task<SpotifyAuth> FixAuth(SpotifyAuth spotifyAuth)
    {
        // Step 1: Generate Authorization URL (for first-time setup only)
        string[] scopes = { "user-read-private","user-read-email","user-read-playback-state","user-modify-playback-state",
            "playlist-read-private","playlist-read-collaborative","playlist-modify-public","playlist-modify-private",
            "user-library-modify","user-library-read", "user-read-currently-playing"};

        string authUrl = GetAuthorizationUrl(scopes, spotifyAuth.ClientId, "https://127.0.0.1:4202/callback");

        Console.WriteLine("Open this URL to authorize the app:");
        Console.WriteLine(authUrl);

        // Wait for the user to provide the authorization code
        Console.WriteLine("Enter the authorization code:");
        string authCode = Console.ReadLine();

        // Step 2: Exchange authorization code for tokens
        //spotifyAuth = await ExchangeCodeForTokensAsync(spotifyAuth.Token, spotifyAuth);
        spotifyAuth = await ExchangeCodeForTokensAsync(authCode, spotifyAuth);
        Console.WriteLine($"Access Token: {spotifyAuth.Token}");
        Console.WriteLine($"Refresh Token: {spotifyAuth.RefreshToken}");

        // Step 3: Reuse refresh token to obtain new access tokens
        string newAccessToken = await RefreshAccessTokenAsync(spotifyAuth);
        spotifyAuth.Token = newAccessToken;
        
        return spotifyAuth;
    }
    */
    
    // private string GetAuthorizationUrl(string[] scopes, string clientId, string redirectUri)
    // {
    //     string scopeString = string.Join("%20", scopes);
    //     return $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientId}" +
    //            $"&scope={scopeString}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
    // }
    
    /*private async Task<SpotifyAuth> ExchangeCodeForTokensAsync(string authCode, SpotifyAuth spotifyAuth)
    {
        using var client = new HttpClient();
        var requestBody = new StringContent(
            $"grant_type=authorization_code&code={authCode}&redirect_uri={Uri.EscapeDataString("https://127.0.0.1:4202/callback")}",
            Encoding.UTF8, "application/x-www-form-urlencoded");

        client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader(spotifyAuth.ClientId, spotifyAuth.ClientSecret));

        var response = await client.PostAsync("https://accounts.spotify.com/api/token", requestBody);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        var tokenData = JsonConvert.DeserializeObject<TokenResponse>(responseData);

        return new SpotifyAuth
        {
            ClientId = spotifyAuth.ClientId,
            ClientSecret = spotifyAuth.ClientSecret,
            RefreshToken = tokenData.RefreshToken,
            Token = tokenData.AccessToken
        };
    }*/
    
    private async Task<TokenResponse> RefreshAccessTokenAsync(SpotifyAuth spotifyAuth)
    {
        using HttpClient client = _HttpMessageHandler is null
            ? new HttpClient()
            : new HttpClient(_HttpMessageHandler, disposeHandler: false);
        // Refresh tokens can contain characters that are not form-body safe (-, _, =) — encode them so a
        // valid token is never rejected as a malformed grant.
        StringContent requestBody = new StringContent(
            $"grant_type=refresh_token&refresh_token={Uri.EscapeDataString(spotifyAuth.RefreshToken ?? "")}",
            Encoding.UTF8, "application/x-www-form-urlencoded");

        client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader(spotifyAuth.ClientId, spotifyAuth.ClientSecret));

        HttpResponseMessage response = await client.PostAsync("https://accounts.spotify.com/api/token", requestBody);
        string responseData = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Spotify says exactly what is wrong in the body (e.g. "invalid_grant" = the refresh token was
            // revoked and needs a one-time re-auth). Surfacing it beats a bare 400.
            string reason = $"token refresh failed: {(int)response.StatusCode} {response.ReasonPhrase} — {responseData}";
            _Logger.LogError($"[SPOTIFY] {reason}");
            throw new SpotifyAuthException(reason, IsInvalidGrant(responseData));
        }

        TokenResponse? tokenData = JsonConvert.DeserializeObject<TokenResponse>(responseData);

        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken))
            throw new SpotifyAuthException("token refresh returned no access_token");

        return tokenData;
    }

    // invalid_grant means the refresh token is permanently dead (revoked, or the client secret was
    // rotated out from under it). Anything else — 500s, rate limits, network blips — is transient.
    private static bool IsInvalidGrant(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return false;

        try
        {
            string? error = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody)
                ?.GetValueOrDefault("error");
            return string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            // Non-JSON body (proxy error page, truncated response) — fall back to a substring check
            // rather than treating an unparseable body as transient forever.
            return responseBody.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase);
        }
    }
    
    private static string GetBasicAuthHeader(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret)) return "";
        
        string credentials = $"{clientId}:{clientSecret}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
    }
}