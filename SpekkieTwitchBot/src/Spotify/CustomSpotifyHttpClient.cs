using System.Net.Http.Headers;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.Auth;

namespace SpekkieTwitchBot.Spotify;

public class CustomSpotifyHttpClient
{
    private readonly HttpClient _Client;
    private readonly SpotifyAuthService _SpotifyAuthService;
    
    public CustomSpotifyHttpClient(SpotifyAuthService spotifyAuthService)
    {
        _Client = new HttpClient();
        _SpotifyAuthService = spotifyAuthService;
        Setup();
    }

    private void Setup()
    {
        SpotifyAuth spotifyAuth = _SpotifyAuthService.GetSpotifyAuth();
        var tokenResponse = _SpotifyAuthService.GetSpotifyToken(_Client, spotifyAuth);
        _Client.DefaultRequestHeaders.Add("client-id", spotifyAuth.ClientId);
        _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _Client.GetAsync(url);
    }
    
    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content)
    {
        return await _Client.PutAsync(url, content);
    }
    
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
    {
        return await _Client.SendAsync(message);
    }
    
    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content)
    {
        return await _Client.PostAsync(url, content);
    }
    
    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        try
        {
            HttpResponseMessage response = await _Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] content = await response.Content.ReadAsByteArrayAsync();

            return content;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving data: {ex.Message}");
            throw;
        }
    }
}