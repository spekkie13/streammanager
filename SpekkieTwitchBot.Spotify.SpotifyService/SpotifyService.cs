using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;
using SpotifyAuthService.General;

namespace SpotifyAuthService;

public class SpotifyService : ISpotifyService
{
    private readonly CustomSpotifyHttpClient _client;
    private readonly Logger _logger;

    public SpotifyService(CustomSpotifyHttpClient spotifyHttpClient, Logger logger)
    {
        _client = spotifyHttpClient;
        _logger = logger;
    }

    public async Task<(CurrentlyPlaying? playable, FullTrack? song)> GetCurrentPlayableAsync(CancellationToken ct = default)
    {
        CurrentlyPlaying? playable = await _client.GetCurrentlyPlayingTrack(SpotifyConstants.CurrentlyPlayingUrl, ct).ConfigureAwait(false);
        FullTrack? song = await _client.GetFullTrack(SpotifyConstants.CurrentlyPlayingUrl, ct).ConfigureAwait(false);
        return (playable, song);
    }

    public async Task<string> GetNowPlayingAsync(CancellationToken ct = default)
    {
        (CurrentlyPlaying? _, FullTrack? song) = await GetCurrentPlayableAsync(ct).ConfigureAwait(false);
        string artists = JoinArtists(song);
        return $"{song?.Name} by {artists}";
    }

    public async Task<string> GetCurrentlyPlayingPlaylistAsync(CancellationToken ct = default)
    {
        CurrentlyPlaying? currentlyPlaying = await _client.DecipherData<CurrentlyPlaying>(SpotifyConstants.CurrentlyPlayingUrl, ct)
            .ConfigureAwait(false);

        Context? ctx = currentlyPlaying?.Context;
        string id = ctx?.Uri.Split(':').Last() ?? "";
        return $"https://open.spotify.com/{ctx?.Type}/{id}";
    }

    public async Task<string> GetQueueAsync(CancellationToken ct = default)
    {
        QueueResponse? songQueue = await _client.DecipherData<QueueResponse>(SpotifyConstants.GetQueueUrl, ct)
            .ConfigureAwait(false);

        if (songQueue == null) return string.Empty;

        int take = Math.Min(10, songQueue.Queue.Count);
        if (take == 0) return string.Empty;

        List<string> parts = new List<string>(take);
        for (int i = 0; i < take; i++)
        {
            FullTrack track = songQueue.Queue.ElementAt(i);
            parts.Add($"{i + 1}: {track.Name} - {JoinArtists(track)}");
        }

        return string.Join(", ", parts);
    }

    public async Task<bool> PlaySpecificSongAsync(string song, CancellationToken ct = default)
    {
        string successAdded = await AddSongToQueueAsync(song, ct).ConfigureAwait(false);
        bool successSkipped = await SkipNextSongAsync(ct).ConfigureAwait(false);
        return !successAdded.Equals("Error", StringComparison.OrdinalIgnoreCase) && successSkipped;
    }

    public async Task<bool> PausePlayerAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response = await _client.PutAsync(SpotifyConstants.PausePlayerUrl, null, ct)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode) return true;

        _logger.LogError($"Error pausing the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
        return false;
    }

    public async Task<bool> ResumePlayerAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response = await _client.PutAsync(SpotifyConstants.StartPlayerUrl, null, ct)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode) return true;

        _logger.LogError($"Error resuming the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
        return false;
    }

    public async Task<bool> SkipNextSongAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response = await _client.PostAsync(SpotifyConstants.SkipNextUrl, null, ct)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode) return true;

        _logger.LogError($"Error skipping to the next song: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
        return false;
    }

    public async Task<bool> SkipPrevSongAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response = await _client.PostAsync(SpotifyConstants.SkipPrevUrl, null, ct)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode) return true;

        _logger.LogError($"Error skipping to the previous song: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
        return false;
    }

    public async Task<string> AddSongToQueueAsync(string songUri, CancellationToken ct = default)
    {
        string songId;

        if (songUri.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase))
        {
            songId = songUri;
        }
        else if (songUri.StartsWith("http", StringComparison.OrdinalIgnoreCase) || songUri.Contains("open.spotify.com"))
        {
            string[] songParts = songUri.Split('/');
            string last = songParts.Last();
            int q = last.IndexOf('?', StringComparison.Ordinal);
            string idPart = q >= 0 ? last[..q] : last;
            songId = $"spotify:track:{idPart}";
        }
        else
        {
            string searchUrl = $"{SpotifyConstants.SpotifySearchUrl}{Uri.EscapeDataString(songUri)}&type=track&limit=1";
            List<Track>? results = await _client.InterpretSongSearchResult(searchUrl, ct).ConfigureAwait(false);
            Track? track = results?.FirstOrDefault();
            if (track?.Uri == null)
            {
                _logger.LogError($"No Spotify track found for search query: {songUri}");
                return "Error";
            }
            songId = track.Uri;
        }

        HttpRequestMessage request = new(HttpMethod.Post, $"{SpotifyConstants.AddToQueueUrl}{songId}");
        HttpResponseMessage response = await _client.SendAsync(request, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string trackId = songId.Replace("spotify:track:", "", StringComparison.OrdinalIgnoreCase);
            string displayName = await _client.GetTrackDisplayNameAsync(trackId, ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(displayName) ? "Success" : displayName;
        }

        _logger.LogError($"Error adding the requested song to the queue: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
        return "Error";
    }

    public async Task<byte[]?> GetCurrentAlbumArtBytesAsync(CurrentlyPlaying? currentlyPlaying, CancellationToken ct = default)
    {
        FullTrack? currentSong = currentlyPlaying?.Item;
        if (currentSong?.Album?.Images == null || currentSong.Album.Images.Count == 0) return null;

        string url = currentSong.Album.Images.First().Url ?? "";
        if (string.IsNullOrWhiteSpace(url)) return null;

        return await _client.GetByteArrayAsync(url, ct).ConfigureAwait(false);
    }

    private static string JoinArtists(FullTrack? currentSong)
    {
        if (currentSong?.Artists == null || currentSong.Artists.Count == 0) return "";

        // netter/veiliger dan handmatige concatenation
        return string.Join(" & ", currentSong.Artists.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
