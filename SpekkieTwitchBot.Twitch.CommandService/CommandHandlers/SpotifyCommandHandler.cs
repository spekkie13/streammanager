using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService;

namespace CommandService.CommandHandlers;

public class SpotifyCommandHandler(SpotifyService spotifyService, SpotifyFileWriter spotifyFileWriter, SpotifySearchService spotifySearchService, IrcClient ircClient)
{
    public void HandleGetCurrentSongCommand()
    {
        var currentSong = spotifyService.GetNowPlaying();
        ircClient.SendPublicChatMessage($"The current song is {currentSong}");
    }

    public void HandleGetCurrentPlaylistCommand()
    {
        var currentPlaylistUrl = spotifyService.GetCurrentlyPlayingPlaylist();
        ircClient.SendPublicChatMessage($"The current playlist is {currentPlaylistUrl}");
    }

    public void HandlePauseMusicCommand()
    {
        var success = spotifyService.PausePlayer().Result;
        var message = success ? "Player paused..." : "Player not paused due to an error...";
        ircClient.SendPublicChatMessage(message);
    }

    public void HandleResumeMusicCommand()
    {
        var success = spotifyService.ResumePlayer().Result;
        var message = success ? "Player resumed..." : "Player not resumed due to an error...";
        ircClient.SendPublicChatMessage(message);
    }

    public void HandleNextSongCommand()
    {
        var success = spotifyService.SkipNextSong().Result;
        var currentSong = spotifyService.GetNowPlaying();
        spotifyFileWriter.WriteSongFile(currentSong);

        ircClient.SendPublicChatMessage(success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...");
    }

    public void HandlePrevSongCommand()
    {
        var success = spotifyService.SkipPrevSong().Result;
        var currentSong = spotifyService.GetNowPlaying();
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
                var message = success ? "Added song to the queue..." : "Could not add song to the queue...";
                ircClient.SendPublicChatMessage(message);
                return success;
            }
        }
        else if (songData.Contains("open.spotify.com"))
        {
            success = spotifyService.AddSongToQueue(songData).Result;
            var message = success ? "Added song to the queue..." : "Could not add song to the queue...";
            ircClient.SendPublicChatMessage(message);
            return success;
        }

        ircClient.SendPublicChatMessage("Please provide a valid spotify link");
        return false;
    }

    public void HandlePlaySpecificSongCommand(string song, string username)
    {
        if (!username.Equals("spekkie1313", StringComparison.CurrentCultureIgnoreCase)) return;
        var success = spotifyService.PlaySpecificSong(song).Result;
        ircClient.SendPublicChatMessage(success ? $"started song: {song}" : $"failed to start song: {song}");
    }

    public void HandleGetQueueCommand()
    {
        var queue = spotifyService.GetQueue();

        ircClient.SendPublicChatMessage($"current queue: {queue}");
    }
}