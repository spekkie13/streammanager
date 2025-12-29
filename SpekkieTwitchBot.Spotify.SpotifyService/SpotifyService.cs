using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Spotify;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService.General;

namespace SpotifyAuthService;

public class SpotifyService : BackgroundService
{
    private readonly CustomSpotifyHttpClient _Client;
    private readonly Logger _Logger;
    private readonly SpotifyFileWriter _SpotifyFileWriter;
    private CurrentlyPlaying? _CurrentPlayable;
    private FullTrack? _CurrentSong;

    public SpotifyService(
        SpotifyFileWriter spotifyFileWriter,
        SpotifyFileSetup spotifyFileSetup,
        CustomSpotifyHttpClient spotifyHttpClient,
        Logger logger)
    {
        _Client = spotifyHttpClient;
        _Logger = logger;
        _SpotifyFileWriter = spotifyFileWriter;
        spotifyFileSetup.SetupSongFiles();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                GetCurrentPlayable();
                UpdateSongImg(_CurrentPlayable);
                _SpotifyFileWriter.WriteSongFile(GetNowPlaying());

                int durationLeft = _CurrentSong?.DurationMs - _CurrentPlayable?.ProgressMs ?? 10000;
                await Task.Delay(TimeSpan.FromMilliseconds(durationLeft), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            _Logger.LogInfo("Spotify service canceled.");
        }
        catch (Exception ex)
        {
            _Logger.LogError("An error occured in spotify Service: " + ex.Message);
            _Logger.LogError(ex.StackTrace);
        }
    }
    
    public string GetNowPlaying()
    {
        GetCurrentPlayable();
        GetCurrentlyPlayingPlaylist();
        string artists = JoinArtists(_CurrentSong);
        return $"{_CurrentSong?.Name} by {artists}";
    }

    private void GetCurrentPlayable()
    {
        _CurrentPlayable = _Client.GetCurrentlyPlayingTrack(SpotifyConstants.CurrentlyPlayingUrl).Result;
        _CurrentSong = _Client.GetFullTrack(SpotifyConstants.CurrentlyPlayingUrl).Result;
    }

    public async Task<bool> PlaySpecificSong(string song)
    {
        bool successAdded = await AddSongToQueue(song);
        bool successSkipped = await SkipNextSong();
        return successAdded && successSkipped;
    }

    public async Task<bool> PausePlayer()
    {
        HttpResponseMessage response = await _Client.PutAsync(SpotifyConstants.PausePlayerUrl, null);

        if (response.IsSuccessStatusCode)
            return true;

        _Logger.LogError(
            $"Error pausing the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> ResumePlayer()
    {
        HttpResponseMessage response = await _Client.PutAsync(SpotifyConstants.StartPlayerUrl, null);

        if (response.IsSuccessStatusCode)
            return true;

        _Logger.LogError(
            $"Error resuming the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> SkipNextSong()
    {
        HttpResponseMessage response = await _Client.PostAsync(SpotifyConstants.SkipNextUrl, null);

        if (response.IsSuccessStatusCode)
            return true;

        _Logger.LogError(
            $"Error skipping to the next song: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> SkipPrevSong()
    {
        HttpResponseMessage response = await _Client.PostAsync(SpotifyConstants.SkipPrevUrl, null);
        if (response.IsSuccessStatusCode)
            return true;

        _Logger.LogError(
            $"Error skipping to the previous song: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> AddSongToQueue(string songUri)
    {
        string songId;
        if (!songUri.StartsWith("spotify:track:"))
        {
            string[] songParts = songUri.Split('/');
            int lastIndex = songParts.Last().IndexOf('?');
            songId = $"spotify:track:{songParts.Last()[..lastIndex]}";
        }
        else
        {
            songId = songUri;
        }

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{SpotifyConstants.AddToQueueUrl}{songId}");

        HttpResponseMessage response = await _Client.SendAsync(request);

        if (response.IsSuccessStatusCode)
            return true;

        _Logger.LogError($"Error adding the requested song to the queue: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    private void UpdateSongImg(CurrentlyPlaying? currentlyPlaying)
    {
        FullTrack? currentSong = currentlyPlaying?.Item;
        if (currentSong?.Album?.Images == null || currentSong.Album.Images.Count == 0) return;

        string url = $"{currentSong.Album.Images.First().Url}";
        byte[] imageBytes = _Client.GetByteArrayAsync(url).Result;
        _SpotifyFileWriter.WriteCurrentSongImage(imageBytes);
    }

    public string GetCurrentlyPlayingPlaylist()
    {
        CurrentlyPlaying? currentlyPlaying = _Client.DecipherData<CurrentlyPlaying>(SpotifyConstants.CurrentlyPlayingUrl).Result;
        Context? currentlyPlayingContext = currentlyPlaying?.Context;
        string id = currentlyPlayingContext?.Uri.Split(':').Last() ?? "";
        string playlistUrl = $"https://open.spotify.com/{currentlyPlayingContext?.Type}/{id}";

        return playlistUrl;
    }

    public string GetQueue()
    {
        QueueResponse? songQueue = _Client.DecipherData<QueueResponse>(SpotifyConstants.GetQueueUrl).Result;
        if (songQueue == null) return string.Empty;

        string queue = "";
        int songIdx = 1;
        for (int i = 0; i < 10; i++)
        {
            FullTrack track = songQueue.Queue.ElementAt(i);
            if (i < 9)
                queue += $"{songIdx}: {track.Name} - {JoinArtists(track)}, ";
            else
                queue += $"{songIdx}: {track.Name} - {JoinArtists(track)}";
            songIdx++;
        }

        return queue;
    }

    private static string JoinArtists(FullTrack? currentSong)
    {
        string artists = "";
        if (currentSong?.Artists == null) return "";
        foreach (SimpleArtist artist in currentSong.Artists)
            if (artist == currentSong.Artists[0])
                artists += $"{artist.Name}";
            else
                artists += $" & {artist.Name}";

        return artists;
    }
}