using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify.Song;
using SpotifyAuthService.General;

namespace SpotifyAuthService;

public class SpotifySearchService(CustomSpotifyHttpClient customSpotifyHttpClient)
{
    public async Task<Track?> GetSongsByName(string songName, string artist = "")
    {
        string url = string.IsNullOrEmpty(artist)
            ? $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}&type=track"
            : $"{SpotifyConstants.SpotifySearchUrl}remaster%2520track%3A{songName}%2520artist%3A{artist}&type=track";

        List<Track>? tracks = await customSpotifyHttpClient.InterpretSongSearchResult(url);
        if (string.IsNullOrEmpty(artist))
        {
            return tracks?.First(t => t.Name == songName);
        }

        return tracks?.Where(t => t.Name == songName)
            .First(t => t.Artists.Any(a => a.Name == artist));
    }
}