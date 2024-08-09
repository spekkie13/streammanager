using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify.Song;

namespace SpekkieTwitchBot.Spotify;

public class SpotifySearchService
{
    private readonly CustomSpotifyHttpClient _CustomSpotifyHttpClient;
    
    public SpotifySearchService(CustomSpotifyHttpClient customSpotifyHttpClient)
    {
        _CustomSpotifyHttpClient = customSpotifyHttpClient;
    }
    
    public async Task<Tracks?> GetSongsByName(string songName, string artist = "")
    {
        string url = string.IsNullOrEmpty(artist) ? 
            $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}&type=track" : 
            $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}%2520artist%3A{artist}&type=track";
        
        HttpResponseMessage message = await _CustomSpotifyHttpClient.GetAsync(url);
        string result = await message.Content.ReadAsStringAsync();
        SongResponse? response = JsonConvert.DeserializeObject<SongResponse>(result);
        
        return response?.Tracks;
    }
}