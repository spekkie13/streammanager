using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;

namespace SpotifyAuthService;

public sealed class SpotifyHostedService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    // Nothing to retry while waiting on a human — just tick slowly enough to notice the re-auth.
    private static readonly TimeSpan ReauthPollDelay = TimeSpan.FromMinutes(1);

    private readonly SpotifyService _Spotify;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private readonly Logger _Logger;
    private bool _FailureLogged;
    private bool _ReauthLogged;

    public SpotifyHostedService(
        SpotifyService spotify,
        SpotifyFileWriter spotifyFileWriter,
        SpotifyFileSetup spotifyFileSetup,
        Logger logger)
    {
        _Spotify = spotify;
        _SpotifyFileWriter = spotifyFileWriter;
        _Logger = logger;
        _ = spotifyFileSetup; // resolved to run file setup on boot
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // kleine delay zodat alles netjes opstart
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                // A single bad tick (expired auth, Spotify hiccup) used to throw straight out of
                // ExecuteAsync and kill the BackgroundService for the rest of the session — now-playing
                // stayed dead until a restart. Each tick is isolated so the loop survives.
                int durationLeft;
                try
                {
                    durationLeft = await TickAsync(stoppingToken).ConfigureAwait(false);
                    _FailureLogged = false;
                    _ReauthLogged = false;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Auth.SpotifyAuthException ex) when (ex.RequiresReauthorization)
                {
                    // CustomSpotifyHttpClient already logged the actionable instructions once, and it
                    // stops issuing token requests until Spotify.json changes — so keep looping (that
                    // is what notices the fix) but stay quiet and cheap in the meantime.
                    if (!_ReauthLogged)
                    {
                        _ReauthLogged = true;
                        _Logger.LogError("[SPOTIFY-HOST] now-playing paused until Spotify is re-authorized.");
                    }

                    durationLeft = (int)ReauthPollDelay.TotalMilliseconds;
                }
                catch (Exception ex)
                {
                    // Log the first failure of a streak only; the underlying auth layer already backs off.
                    if (!_FailureLogged)
                    {
                        _FailureLogged = true;
                        _Logger.LogError("[SPOTIFY-HOST] tick failed, retrying: " + ex.Message);
                    }

                    durationLeft = (int)RetryDelay.TotalMilliseconds;
                }

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

    // Runs one poll and returns how long to wait before the next one (roughly the remaining track time).
    private async Task<int> TickAsync(CancellationToken stoppingToken)
    {
        (CurrentlyPlaying? playable, FullTrack? song) = await _Spotify.GetCurrentPlayableAsync(stoppingToken).ConfigureAwait(false);

        // album art -> file
        byte[]? artBytes = await _Spotify.GetCurrentAlbumArtBytesAsync(playable, stoppingToken).ConfigureAwait(false);
        if (artBytes is { Length: > 0 })
            _SpotifyFileWriter.WriteCurrentSongImage(artBytes);

        // wacht ongeveer tot track klaar is (maar met safety clamp)
        int durationLeft = (song?.DurationMs ?? 10000) - (playable?.ProgressMs ?? 0);
        durationLeft = Math.Clamp(durationLeft, 2_000, 60_000); // min 2s, max 60s (voorkomt idiote delays)

        // now playing -> file
        string nowPlaying = $"{song?.Name} by {GetArtists(song)}";
        _SpotifyFileWriter.WriteSongFile(nowPlaying);
        _SpotifyFileWriter.WriteNowPlayingHtml(song?.Name ?? "", GetArtists(song), durationLeft + 1000);

        return durationLeft;
    }

    private static string GetArtists(FullTrack? song)
    {
        if (song?.Artists == null || song.Artists.Count == 0) return "";
        return string.Join(" & ", song.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
