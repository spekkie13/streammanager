using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Spotify;
using SpekkieTwitchBot.Spotify.FileHandling;
using SpotifyAPI.Web;

namespace SpekkieTwitchBot.Auth;

public class SpotifyAuthService
{
    private static SpotifyAuth? _SpotifyAuth;
    private readonly SpotifyFileReader _SpotifyFileReader;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private readonly Logger _Logger;
    
    public SpotifyAuthService(
        SpotifyFileWriter spotifyFileWriter,
        SpotifyFileReader spotifyFileReader,
        Logger logger)
    {
        _SpotifyFileWriter = spotifyFileWriter;
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
    
    private async Task<TokenResponse?> RefreshSpotifyAccessToken(string clientId, string clientSecret, string refreshToken, HttpClient client)
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

        _Logger.LogError($"Error refreshing access token: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return null;
    }
}