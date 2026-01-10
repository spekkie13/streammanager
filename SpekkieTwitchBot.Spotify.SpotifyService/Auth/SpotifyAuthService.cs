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
    private readonly Logger _Logger;
    private static SpotifyAuth? _SpotifyAuth;

    public SpotifyAuthService(SpotifyFileReader spotifyFileReader, Logger logger)
    {
        _SpotifyFileReader = spotifyFileReader;
        _Logger = logger;
    }
    
    public SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = _SpotifyFileReader.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();
        _Logger.LogInfo($"Spotify Auth: {JsonConvert.SerializeObject(_SpotifyAuth)}");
        return _SpotifyAuth;
    }
    
    public async Task<SpotifyAuth> FixAuth(SpotifyAuth spotifyAuth)
    {
        string newAccessToken = await RefreshAccessTokenAsync(spotifyAuth);
        spotifyAuth.Token = newAccessToken;
        
        return spotifyAuth;
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
    
    private async Task<string> RefreshAccessTokenAsync(SpotifyAuth spotifyAuth)
    {
        using HttpClient client = new HttpClient();
        StringContent requestBody = new StringContent(
            $"grant_type=refresh_token&refresh_token={spotifyAuth.RefreshToken}",
            Encoding.UTF8, "application/x-www-form-urlencoded");

        client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader(spotifyAuth.ClientId, spotifyAuth.ClientSecret));

        HttpResponseMessage response = await client.PostAsync("https://accounts.spotify.com/api/token", requestBody);
        response.EnsureSuccessStatusCode();

        string responseData = await response.Content.ReadAsStringAsync();
        TokenResponse? tokenData = JsonConvert.DeserializeObject<TokenResponse>(responseData);

        return tokenData?.AccessToken ?? "";
    }
    
    private static string GetBasicAuthHeader(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret)) return "";
        
        string credentials = $"{clientId}:{clientSecret}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
    }
}