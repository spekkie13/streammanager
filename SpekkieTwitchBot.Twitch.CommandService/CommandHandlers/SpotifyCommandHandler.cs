using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService;

namespace CommandService.CommandHandlers;

public class SpotifyCommandHandler(
    SpotifyService spotifyService, 
    SpotifyFileWriter spotifyFileWriter, 
    SpotifySearchService spotifySearchService)
{
    public string HandleGetCurrentSongCommand()
    {
        string currentSong = spotifyService.GetNowPlaying();
        return $"The current song is {currentSong}";
    }

    public string HandleGetCurrentPlaylistCommand()
    {
        string currentPlaylistUrl = spotifyService.GetCurrentlyPlayingPlaylist();
        return $"The current playlist is {currentPlaylistUrl}";
    }

    public string HandlePauseMusicCommand()
    {
        bool success = spotifyService.PausePlayer().Result;
        return success ? "Player paused..." : "Player not paused due to an error...";
    }

    public string HandleResumeMusicCommand()
    {
        bool success = spotifyService.ResumePlayer().Result;
        return success ? "Player resumed..." : "Player not resumed due to an error...";
    }

    public string HandleNextSongCommand()
    {
        bool success = spotifyService.SkipNextSong().Result;
        string currentSong = spotifyService.GetNowPlaying();
        spotifyFileWriter.WriteSongFile(currentSong);

        return success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...";
    }

    public string HandlePrevSongCommand()
    {
        bool success = spotifyService.SkipPrevSong().Result;
        string currentSong = spotifyService.GetNowPlaying();
        spotifyFileWriter.WriteSongFile(currentSong);

        return success
            ? "Skipped to the previous song..."
            : "Failed to skip to the previous song...";
    }

    public string HandleAddSongToQueueCommand(string songData)
    {
        bool success;
        if (songData.Split("|").Length == 2)
        {
            string title = songData.Split("|")[0];
            string artist = songData.Split("|")[1];
            Track? song = spotifySearchService.GetSongsByName(title, artist).Result;
            if (song != null)
            {
                string uri = song.Uri ?? "";
                success = spotifyService.AddSongToQueue(uri).Result;
                return success ? "Added song to the queue..." : "Could not add song to the queue...";
            }
        }
        else if (songData.Contains("open.spotify.com"))
        {
            success = spotifyService.AddSongToQueue(songData).Result;
            return success ? "Added song to the queue..." : "Could not add song to the queue...";
        }

        return "Please provide a valid spotify link";
    }

    public string HandlePlaySpecificSongCommand(string song, string username)
    {
        if (!username.Equals("itsspekkie", StringComparison.CurrentCultureIgnoreCase)) return "";
        bool success = spotifyService.PlaySpecificSong(song).Result;
        return success ? $"started song: {song}" : $"failed to start song: {song}";
    }

    public string HandleGetQueueCommand()
    {
        string queue = spotifyService.GetQueue();
        return $"current queue: {queue}";
    }
}