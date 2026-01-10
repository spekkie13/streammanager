using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class SpotifyCommandHandler
{
    private readonly SpotifyService _SpotifyService;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private readonly SpotifySearchService _SpotifySearchService;
    
    public SpotifyCommandHandler(
        SpotifyService spotifyService,
        SpotifyFileWriter spotifyFileWriter,
        SpotifySearchService spotifySearchService
    )
    {
        _SpotifyService = spotifyService;
        _SpotifyFileWriter = spotifyFileWriter;
        _SpotifySearchService = spotifySearchService;
    }
    
    public async Task<string> HandleGetCurrentSongCommand(CancellationToken cancellationToken = default)
    {
        string currentSong = await _SpotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        return $"The current song is {currentSong}";
    }

    public async Task<string> HandleGetCurrentPlaylistCommand(CancellationToken cancellationToken = default)
    {
        string currentPlaylistUrl = await _SpotifyService.GetCurrentlyPlayingPlaylistAsync(cancellationToken).ConfigureAwait(false);
        return $"The current playlist is {currentPlaylistUrl}";
    }

    public async Task<string> HandlePauseMusicCommand(CancellationToken cancellationToken = default)
    {
        bool success = await _SpotifyService.PausePlayerAsync(cancellationToken).ConfigureAwait(false);
        return success ? "Player paused..." : "Player not paused due to an error...";
    }

    public async Task<string> HandleResumeMusicCommand(CancellationToken cancellationToken = default)
    {
        bool success = await _SpotifyService.ResumePlayerAsync(cancellationToken).ConfigureAwait(false);
        return success ? "Player resumed..." : "Player not resumed due to an error...";
    }

    public async Task<string> HandleNextSongCommand(CancellationToken cancellationToken = default)
    {
        bool success = await _SpotifyService.SkipNextSongAsync(cancellationToken).ConfigureAwait(false);
        string currentSong = await _SpotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        _SpotifyFileWriter.WriteSongFile(currentSong);

        return success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...";
    }

    public async Task<string> HandlePrevSongCommand(CancellationToken cancellationToken = default)
    {
        bool success = await _SpotifyService.SkipPrevSongAsync(cancellationToken).ConfigureAwait(false);
        string currentSong = await _SpotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        _SpotifyFileWriter.WriteSongFile(currentSong);

        return success
            ? "Skipped to the previous song..."
            : "Failed to skip to the previous song...";
    }

    public async Task<string> HandleAddSongToQueueCommand(string songData, CancellationToken cancellationToken = default)
    {
        string result;
        if (songData.Split("|").Length == 2)
        {
            string title = songData.Split("|")[0];
            string artist = songData.Split("|")[1];
            Track? song = _SpotifySearchService.GetSongsByName(title, artist).GetAwaiter().GetResult();
            if (song != null)
            {
                string uri = song.Uri ?? "";
                result = await _SpotifyService.AddSongToQueueAsync(uri, cancellationToken).ConfigureAwait(false);
                return result == "Success" ? "Added song to the queue..." : "Could not add song to the queue...";
            }
        }
        else if (songData.Contains("open.spotify.com"))
        {
            result = await _SpotifyService.AddSongToQueueAsync(songData, cancellationToken).ConfigureAwait(false);
            return result == "Success" ? "Added song to the queue..." : "Could not add song to the queue...";
        }

        return "Please provide a valid spotify link";
    }

    public async Task<string> HandlePlaySpecificSongCommand(string song, string username, CancellationToken cancellationToken = default)
    {
        if (!username.Equals("itsspekkie", StringComparison.CurrentCultureIgnoreCase)) return "";
        bool success = await _SpotifyService.PlaySpecificSongAsync(song, cancellationToken);
        return success ? $"started song: {song}" : $"failed to start song: {song}";
    }

    public async Task<string> HandleGetQueueCommand(CancellationToken cancellationToken = default)
    {
        string queue = await _SpotifyService.GetQueueAsync(cancellationToken);
        return $"current queue: {queue}";
    }
}