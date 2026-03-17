using SpekkieClassLibrary.Spotify.Song;

namespace SpotifyAuthService;

public interface ISpotifySearchService
{
    Task<Track?> GetSongsByName(string songName, string artist = "");
}
