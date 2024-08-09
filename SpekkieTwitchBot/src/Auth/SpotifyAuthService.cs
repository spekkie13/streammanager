using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Spotify.FileHandling;

namespace SpekkieTwitchBot.Auth;

public class SpotifyAuthService
{
    private static SpotifyAuth? _SpotifyAuth;
    private readonly SpotifyFileReader _SpotifyFileReader;
    private readonly Logger _Logger;
    
    public SpotifyAuthService(
        SpotifyFileReader spotifyFileReader,
        Logger logger)
    {
        _SpotifyFileReader = spotifyFileReader;
        _Logger = logger;
    }

    public SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = _SpotifyFileReader.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();
        return _SpotifyAuth;
    }

    public AuthorizationCodeTokenResponse GetSpotifyToken(HttpClient client, SpotifyAuth auth)
    {
        TokenResponse? accessToken = RefreshSpotifyAccessToken(auth.ClientId, auth.ClientSecret, auth.RefreshToken, client).Result;
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
    
    private async Task<TokenResponse?> RefreshSpotifyAccessToken(string clientId, string clientSecret, string refreshToken, HttpClient client)
    {
        FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", clientId },
            { "client_secret", clientSecret }
        });

        HttpResponseMessage response = await client.PostAsync(SpotifyConstants.TokenUrl, content);

        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

            return new TokenResponse { AccessToken = tokenResponse?.AccessToken ?? "", RefreshToken = tokenResponse?.RefreshToken ?? "" };
        }

        _Logger.LogError($"Error refreshing access token: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return null;
    }
}