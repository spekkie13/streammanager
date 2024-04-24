using System.Net.Http.Headers;
using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Constants;

namespace SpekkieTwitchBot.Spotify;

public class SpotifySearchService
{
    private readonly HttpClient _Client;
    
    public SpotifySearchService(SpotifyAuthService spotifyAuthService)
    {
        _Client = new HttpClient();
        
        SpotifyAuth spotifyAuth = spotifyAuthService.GetSpotifyAuth();
        var tokenResponse = spotifyAuthService.GetSpotifyToken(_Client, spotifyAuth);
        _Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
    }
    
    public async Task<Tracks?> GetSongsByName(string songName, string artist = "")
    {
        string url = string.IsNullOrEmpty(artist) ? 
            $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}&type=track" : 
            $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}%2520artist%3A{artist}&type=track";
        
        HttpResponseMessage message = await _Client.GetAsync(url);
        string result = await message.Content.ReadAsStringAsync();
        SongResponse? response = JsonConvert.DeserializeObject<SongResponse>(result);
        
        return response?.Tracks;
    }
}