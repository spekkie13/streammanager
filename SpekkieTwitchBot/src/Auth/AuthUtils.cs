using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Spotify;
using SpekkieTwitchBot.Models.Twitch;
using SpotifyAPI.Web;

namespace SpekkieTwitchBot.Auth;

public static class AuthUtils
{
    private static SpotifyAuth? _SpotifyAuth;
    public static TwitchAuth GetTwitchAuth()
    {
        string jsonData = FileHandler.ReadTwitchAuthFile();
        TwitchAuth auth = JsonConvert.DeserializeObject<TwitchAuth>(jsonData) ?? new TwitchAuth();
        return auth;
    }

    public static SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = FileHandler.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();
        return _SpotifyAuth;
    }

    public static AuthorizationCodeTokenResponse GetSpotifyToken(HttpClient client, SpotifyAuth auth)
    {
        var accessToken = RefreshAccessToken(auth.client_id, auth.client_secret, auth.refresh_token, client).Result;
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
    
    private static async Task<TokenResponse?> RefreshAccessToken(string clientId, string clientSecret, string refreshToken, HttpClient client)
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

}