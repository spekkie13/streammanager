using System.Net;
using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Spotify;
using SpekkieTwitchBot.Models.Twitch;
using SpotifyAPI.Web;

namespace SpekkieTwitchBot.Auth;

public static class AuthService
{
    private static SpotifyAuth? _SpotifyAuth;

    public static SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = FileHandler.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();
        return _SpotifyAuth;
    }

    public static AuthorizationCodeTokenResponse GetSpotifyToken(HttpClient client, SpotifyAuth auth)
    {
        var accessToken = RefreshSpotifyAccessToken(auth.client_id, auth.client_secret, auth.refresh_token, client).Result;
        AuthorizationCodeTokenResponse tokenResponse = new ()
        {
            RefreshToken = accessToken?.RefreshToken ?? "",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "playlist-read-private user-read-currently-playing user-read-playback-state user-modify-playback-state playlist-read-private playlist-read-collaborative",
            AccessToken = accessToken?.AccessToken ?? ""
        };
        return tokenResponse;
    }
    
    private static async Task<TokenResponse?> RefreshSpotifyAccessToken(string clientId, string clientSecret, string refreshToken, HttpClient client)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", clientId },
            { "client_secret", clientSecret }
        });

        var response = await client.PostAsync(SpotifyConstants.TokenUrl, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

            return new TokenResponse { AccessToken = tokenResponse?.AccessToken ?? "", RefreshToken = tokenResponse?.RefreshToken ?? "" };
        }

        Console.WriteLine($"Error refreshing access token: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return null;
    }
    
    public static TwitchAuth GetTwitchAuth()
    {
        string jsonData = FileHandler.ReadTwitchAuthFile();
        TwitchAuth auth = JsonConvert.DeserializeObject<TwitchAuth>(jsonData) ?? new TwitchAuth();
        return auth;
    }

    public static async Task<ClientCredentials?> GetClientCredentials(TwitchAuth twitchAuth)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchAuth.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            ClientCredentials? cred = JsonConvert.DeserializeObject<ClientCredentials>(responseContent);
            UpdateTwitchSettings(twitchAuth, clientCredentials: cred);
            return cred;
        }
        
        Console.WriteLine($"Failed to get access token. Status code: {response.StatusCode}");
        return null;
    }

    public static async Task<AuthorizationCredentials?> GetAuthorizationCredentials(TwitchAuth twitchAuth)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchAuth.ClientSecret),
            new KeyValuePair<string, string>("code", twitchAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                AuthorizationCredentials? cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                UpdateTwitchSettings(twitchAuth, authCred: cred);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshTokenAsync(twitchAuth.ClientId, twitchAuth.ClientSecret, twitchAuth.RefreshToken);
                UpdateTwitchSettings(twitchAuth, authCred: cred);
                return cred;
            default:
                Console.WriteLine($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }
    
    private static async Task<AuthorizationCredentials?> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        Console.WriteLine($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private static void UpdateTwitchSettings(TwitchAuth twitchAuth, AuthorizationCredentials? authCred = null, ClientCredentials? clientCredentials = null)
    {
        if (authCred == null && clientCredentials != null)
        {
            twitchAuth.UserToken = clientCredentials.access_token;
        }

        if (clientCredentials == null && authCred != null)
        {
            twitchAuth.AppToken = authCred.access_token;
            twitchAuth.RefreshToken = authCred.refresh_token;
        }
        
        string json = JsonConvert.SerializeObject(twitchAuth);
        FileHandler.WriteTwitchAuthFile(json);
    }
}