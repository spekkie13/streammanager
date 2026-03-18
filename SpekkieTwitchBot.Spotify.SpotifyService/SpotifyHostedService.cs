using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;

namespace SpotifyAuthService;

public sealed class SpotifyHostedService : BackgroundService
{
    private readonly SpotifyService _Spotify;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private readonly Logger _Logger;

    public SpotifyHostedService(
        SpotifyService spotify,
        SpotifyFileWriter spotifyFileWriter,
        SpotifyFileSetup spotifyFileSetup,
        Logger logger)
    {
        _Spotify = spotify;
        _SpotifyFileWriter = spotifyFileWriter;
        _Logger = logger;

        spotifyFileSetup.SetupSongFiles();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // kleine delay zodat alles netjes opstart
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                (CurrentlyPlaying? playable, FullTrack? song) = await _Spotify.GetCurrentPlayableAsync(stoppingToken).ConfigureAwait(false);

                // album art -> file
                byte[]? artBytes = await _Spotify.GetCurrentAlbumArtBytesAsync(playable, stoppingToken).ConfigureAwait(false);
                if (artBytes is { Length: > 0 })
                    _SpotifyFileWriter.WriteCurrentSongImage(artBytes);

                // now playing -> file
                string nowPlaying = $"{song?.Name} by {GetArtists(song)}";
                _SpotifyFileWriter.WriteSongFile(nowPlaying);
                _SpotifyFileWriter.WriteNowPlayingHtml(song?.Name ?? "", GetArtists(song));

                // wacht ongeveer tot track klaar is (maar met safety clamp)
                int durationLeft = (song?.DurationMs ?? 10000) - (playable?.ProgressMs ?? 0);
                durationLeft = Math.Clamp(durationLeft, 2_000, 60_000); // min 2s, max 60s (voorkomt idiote delays)

                await Task.Delay(TimeSpan.FromMilliseconds(durationLeft), stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _Logger.LogInfo("[SPOTIFY-HOST] canceled.");
        }
        catch (Exception ex)
        {
            _Logger.LogError("[SPOTIFY-HOST] error: " + ex);
        }

        Probe.Log("SpotifyHostedService ExecuteAsync END");
    }

    private static string GetArtists(FullTrack? song)
    {
        if (song?.Artists == null || song.Artists.Count == 0) return "";
        return string.Join(" & ", song.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
