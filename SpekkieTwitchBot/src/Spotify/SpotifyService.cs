using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Spotify;
using SpotifyAPI.Web;

namespace SpekkieTwitchBot.Spotify;

public class SpotifyService : BackgroundService
{
    private readonly HttpClient _Client;
    private FullTrack? _CurrentSong;
    private CurrentlyPlaying? _CurrentPlayable;

    public SpotifyService()
    {
        _Client = new HttpClient();
        SpotifyAuth spotifyAuth = AuthService.GetSpotifyAuth();
        var tokenResponse = AuthService.GetSpotifyToken(_Client, spotifyAuth);
        _Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            GetCurrentPlayable();
            _CurrentSong = GetCurrentSong(_CurrentPlayable);
            UpdateSongImg(_CurrentPlayable);
            FileHandler.WriteSongFile(GetNowPlaying());

            int durationLeft = _CurrentSong?.DurationMs - _CurrentPlayable?.ProgressMs ?? 10000;
            await Task.Delay(TimeSpan.FromMilliseconds(durationLeft), stoppingToken);
        }
    }
    
    public string GetNowPlaying()
    {
        GetCurrentPlayable();
        _CurrentSong = GetCurrentSong(_CurrentPlayable);
        GetCurrentlyPlayingPlaylist();
        string artists = JoinArtists(_CurrentSong);
        return $"{_CurrentSong?.Name} by {artists}";
    }

    private void GetCurrentPlayable()
    {
        string currentlyPlayingData = GetData(SpotifyConstants.CurrentlyPlayingUrl);
        _CurrentPlayable = JsonConvert.DeserializeObject<CurrentlyPlaying>(currentlyPlayingData);
    }
    
    private FullTrack? GetCurrentSong(CurrentlyPlaying? currentlyPlaying)
    {
        FullTrack? currentSong = (FullTrack?) currentlyPlaying?.Item;
        return currentSong;
    }

    public async Task<bool> PlaySpecificSong(string song)
    {
        bool successAdded = await AddSongToQueue(song);
        bool successSkipped = await SkipNextSong();
        return successAdded && successSkipped;
    }

    public async Task<bool> PausePlayer()
    {
        var response = await _Client.PutAsync(SpotifyConstants.PausePlayerUrl, null);

        if (response.IsSuccessStatusCode)
            return true;
        
        Logger.LogError($"Error pausing the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;    
    }

    public async Task<bool> ResumePlayer()
    {
        var response = await _Client.PutAsync(SpotifyConstants.StartPlayerUrl, null);

        if (response.IsSuccessStatusCode)
            return true;
        
        Logger.LogError($"Error resuming the player: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> SkipNextSong()
    {
        var response = await _Client.PostAsync(SpotifyConstants.SkipNextUrl, null);

        if (response.IsSuccessStatusCode)
            return true;
        
        Logger.LogError($"Error skipping to the next song: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;    
    }    
    
    public async Task<bool> SkipPrevSong()
    {
        var response = await _Client.PostAsync(SpotifyConstants.SkipPrevUrl, null);
        if (response.IsSuccessStatusCode)
            return true;
        
        Logger.LogError($"Error skipping to the previous song: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public async Task<bool> AddSongToQueue(string songUri)
    {
        string[] songParts = songUri.Split('/');
        int lastIndex = songParts.Last().IndexOf("?", StringComparison.Ordinal);
        string songId = $"spotify:track:{songParts.Last()[..lastIndex]}";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{SpotifyConstants.AddToQueueUrl}{songId}");

        var response = await _Client.SendAsync(request);

        if (response.IsSuccessStatusCode)
            return true;
        
        Logger.LogError($"Error adding the requested song to the queue: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    private void UpdateSongImg(CurrentlyPlaying? currentlyPlaying)
    {
        FullTrack? currentSong = (FullTrack?) currentlyPlaying?.Item;
        if (currentSong == null) return;
        string url = $"{currentSong.Album.Images.First().Url}";
        var imageBytes = _Client.GetByteArrayAsync(url).Result;
        FileHandler.WriteCurrentSongImage(imageBytes);
    }
    
    public string GetCurrentlyPlayingPlaylist()
    {
        string currentlyPlayingData = GetData(SpotifyConstants.CurrentlyPlayingUrl);
        CurrentlyPlaying? currentlyPlaying = JsonConvert.DeserializeObject<CurrentlyPlaying>(currentlyPlayingData);
        Context? currentlyPlayingContext = currentlyPlaying?.Context;
        string id = currentlyPlayingContext?.Uri.Split(':').Last() ?? "";
        string playlistUrl = $"https://open.spotify.com/{currentlyPlayingContext?.Type}/{id}";
        
        return playlistUrl;
    }

    public string GetQueue()
    {
        string queueData = GetData(SpotifyConstants.GetQueueUrl);
        string queue = "";
        QueueResponse songQueue = JsonConvert.DeserializeObject<QueueResponse>(queueData) ?? new QueueResponse();
        int songIdx = 1;
        for (int i = 0; i < 10; i++)
        {
            FullTrack track = (FullTrack) songQueue.Queue.ElementAt(i);
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
        {
            if (artist == currentSong.Artists[0])
                artists += $"{artist.Name}";
            else
                artists += $" & {artist.Name}";
        }

        return artists;
    }

    private string GetData(string url)
    {
        var myUri = new Uri(url);
        
        var myWebResponse = _Client.GetAsync(myUri).Result;
        var responseStream = myWebResponse.Content.ReadAsStreamAsync().Result;
        var myStreamReader = new StreamReader(responseStream, Encoding.Default);
        var json = myStreamReader.ReadToEnd();

        return json;
    }
}