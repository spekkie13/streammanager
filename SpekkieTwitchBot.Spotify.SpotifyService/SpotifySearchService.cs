using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify.Song;
using SpotifyAuthService.General;

namespace SpotifyAuthService;

public class SpotifySearchService : ISpotifySearchService
{
    private readonly CustomSpotifyHttpClient _customSpotifyHttpClient;

    public SpotifySearchService(CustomSpotifyHttpClient customSpotifyHttpClient)
    {
        _customSpotifyHttpClient = customSpotifyHttpClient;
    }
    
    public async Task<Track?> GetSongsByName(string songName, string artist = "")
    {
        string url = string.IsNullOrEmpty(artist)
            ? $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}&type=track"
            : $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}%2520artist%3A{artist}&type=track";

        List<Track>? tracks = await _customSpotifyHttpClient.InterpretSongSearchResult(url);
        if (string.IsNullOrEmpty(artist))
        {
            return tracks?.First(t => t.Name == songName);
        }

        
        return tracks?.Where(t => t.Name == songName)
            .First(t => t.Artists != null && t.Artists.Any(a => a.Name == artist));
    }
}