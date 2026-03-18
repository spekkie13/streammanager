using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class SpotifyCommandHandler(
    ISpotifyService spotifyService,
    SpotifyFileWriter spotifyFileWriter,
    ISpotifySearchService spotifySearchService,
    ITwitchChannelInfoClient channelInfo)
    : ISpotifyCommandHandler
{
    private readonly HashSet<string> _SongRequestedThisStream = new(StringComparer.OrdinalIgnoreCase);
    private string? _CurrentStreamId;

    public async Task<string> HandleGetCurrentSongCommand(CancellationToken cancellationToken = default)
    {
        string currentSong = await spotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        return $"The current song is {currentSong}";
    }

    public async Task<string> HandleGetCurrentPlaylistCommand(CancellationToken cancellationToken = default)
    {
        string currentPlaylistUrl = await spotifyService.GetCurrentlyPlayingPlaylistAsync(cancellationToken).ConfigureAwait(false);
        return $"The current playlist is {currentPlaylistUrl}";
    }

    public async Task<string> HandlePauseMusicCommand(CancellationToken cancellationToken = default)
    {
        bool success = await spotifyService.PausePlayerAsync(cancellationToken).ConfigureAwait(false);
        return success ? "Player paused..." : "Player not paused due to an error...";
    }

    public async Task<string> HandleResumeMusicCommand(CancellationToken cancellationToken = default)
    {
        bool success = await spotifyService.ResumePlayerAsync(cancellationToken).ConfigureAwait(false);
        return success ? "Player resumed..." : "Player not resumed due to an error...";
    }

    public async Task<string> HandleNextSongCommand(CancellationToken cancellationToken = default)
    {
        bool success = await spotifyService.SkipNextSongAsync(cancellationToken).ConfigureAwait(false);
        string currentSong = await spotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        spotifyFileWriter.WriteSongFile(currentSong);

        return success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...";
    }

    public async Task<string> HandlePrevSongCommand(CancellationToken cancellationToken = default)
    {
        bool success = await spotifyService.SkipPrevSongAsync(cancellationToken).ConfigureAwait(false);
        string currentSong = await spotifyService.GetNowPlayingAsync(cancellationToken).ConfigureAwait(false);
        spotifyFileWriter.WriteSongFile(currentSong);

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
            Track? song = spotifySearchService.GetSongsByName(title, artist).GetAwaiter().GetResult();
            if (song != null)
            {
                string uri = song.Uri ?? "";
                result = await spotifyService.AddSongToQueueAsync(uri, cancellationToken).ConfigureAwait(false);
                return result == "Success" ? "Added song to the queue..." : "Could not add song to the queue...";
            }
        }
        else if (songData.Contains("open.spotify.com"))
        {
            result = await spotifyService.AddSongToQueueAsync(songData, cancellationToken).ConfigureAwait(false);
            return result == "Success" ? "Added song to the queue..." : "Could not add song to the queue...";
        }

        return "Please provide a valid spotify link";
    }

    public async Task<string> HandlePlaySpecificSongCommand(string song, string username, CancellationToken cancellationToken = default)
    {
        if (!username.Equals("itsspekkie", StringComparison.CurrentCultureIgnoreCase)) return "";
        bool success = await spotifyService.PlaySpecificSongAsync(song, cancellationToken);
        return success ? $"started song: {song}" : $"failed to start song: {song}";
    }

    public async Task<string> HandleGetQueueCommand(CancellationToken cancellationToken = default)
    {
        string queue = await spotifyService.GetQueueAsync(cancellationToken);
        return $"current queue: {queue}";
    }

    public async Task<string> HandleSongRequestCommand(string input, string userId, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return $"@{username} Usage: !sr <song name or Spotify link>";

        string? streamId = await channelInfo.GetCurrentStreamIdAsync(ct);
        if (streamId != _CurrentStreamId)
        {
            _CurrentStreamId = streamId;
            _SongRequestedThisStream.Clear();
        }

        if (!_SongRequestedThisStream.Add(userId))
            return $"@{username} You can only use !sr once per stream. Use the channel point reward to request more songs!";

        string result = await spotifyService.AddSongToQueueAsync(input, ct);
        bool success = !result.Equals("Error", StringComparison.OrdinalIgnoreCase);

        if (!success)
            _SongRequestedThisStream.Remove(userId);

        return success
            ? $"@{username} Successfully added {result} to the queue!"
            : $"@{username} Failed to add song to the queue";
    }
}