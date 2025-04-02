using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Spotify;

namespace SpotifyAuthService.Auth;

public class SpotifyAuthService(SpotifyFileReader spotifyFileReader, Logger logger)
{
    private static SpotifyAuth? _SpotifyAuth;
    
    public SpotifyAuth GetSpotifyAuth()
    {
        string jsonData = spotifyFileReader.ReadSpotifyAuthFile();
        _SpotifyAuth = JsonConvert.DeserializeObject<SpotifyAuth>(jsonData) ?? new SpotifyAuth();
        logger.LogInfo($"Spotify Auth: {_SpotifyAuth}");
        return _SpotifyAuth;
    }
    
    public async Task<SpotifyAuth> FixAuth(SpotifyAuth spotifyAuth)
    {
        string newAccessToken = await RefreshAccessTokenAsync(spotifyAuth);
        spotifyAuth.Token = newAccessToken;
        
        return spotifyAuth;
    }
    
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
    
    private static string GetBasicAuthHeader(string clientId, string clientSecret)
    {
        string credentials = $"{clientId}:{clientSecret}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
    }
}
