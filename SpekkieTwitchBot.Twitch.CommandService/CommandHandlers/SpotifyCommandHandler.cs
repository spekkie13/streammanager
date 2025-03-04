using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService;

namespace CommandService.CommandHandlers;

public class SpotifyCommandHandler(SpotifyService spotifyService, SpotifyFileWriter spotifyFileWriter, SpotifySearchService spotifySearchService, IrcClient ircClient)
{
    public void HandleGetCurrentSongCommand()
    {
        string currentSong = spotifyService.GetNowPlaying();
        ircClient.SendPublicChatMessage($"The current song is {currentSong}");
    }

    public void HandleGetCurrentPlaylistCommand()
    {
        string currentPlaylistUrl = spotifyService.GetCurrentlyPlayingPlaylist();
        ircClient.SendPublicChatMessage($"The current playlist is {currentPlaylistUrl}");
    }

    public void HandlePauseMusicCommand()
    {
        bool success = spotifyService.PausePlayer().Result;
        string message = success ? "Player paused..." : "Player not paused due to an error...";
        ircClient.SendPublicChatMessage(message);
    }

    public void HandleResumeMusicCommand()
    {
        bool success = spotifyService.ResumePlayer().Result;
        string message = success ? "Player resumed..." : "Player not resumed due to an error...";
        ircClient.SendPublicChatMessage(message);
    }

    public void HandleNextSongCommand()
    {
        bool success = spotifyService.SkipNextSong().Result;
        string currentSong = spotifyService.GetNowPlaying();
        spotifyFileWriter.WriteSongFile(currentSong);

        ircClient.SendPublicChatMessage(success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...");
    }

    public void HandlePrevSongCommand()
    {
        bool success = spotifyService.SkipPrevSong().Result;
        string currentSong = spotifyService.GetNowPlaying();
        spotifyFileWriter.WriteSongFile(currentSong);

        ircClient.SendPublicChatMessage(success
            ? "Skipped to the previous song..."
            : "Failed to skip to the previous song...");
    }

    public bool HandleAddSongToQueueCommand(string songData)
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
                string message = success ? "Added song to the queue..." : "Could not add song to the queue...";
                ircClient.SendPublicChatMessage(message);
                return success;
            }
        }
        else if (songData.Contains("open.spotify.com"))
        {
            success = spotifyService.AddSongToQueue(songData).Result;
            string message = success ? "Added song to the queue..." : "Could not add song to the queue...";
            ircClient.SendPublicChatMessage(message);
            return success;
        }

        ircClient.SendPublicChatMessage("Please provide a valid spotify link");
        return false;
    }

    public void HandlePlaySpecificSongCommand(string song, string username)
    {
        if (!username.Equals("spekkie1313", StringComparison.CurrentCultureIgnoreCase)) return;
        bool success = spotifyService.PlaySpecificSong(song).Result;
        ircClient.SendPublicChatMessage(success ? $"started song: {song}" : $"failed to start song: {song}");
    }

    public void HandleGetQueueCommand()
    {
        string queue = spotifyService.GetQueue();

        ircClient.SendPublicChatMessage($"current queue: {queue}");
    }
}