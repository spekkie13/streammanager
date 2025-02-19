using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieClassLibrary.Spotify.Song;

namespace SpotifyAuthService;

public class CustomSpotifyHttpClient
{
    private readonly HttpClient _Client;
    private readonly SpotifyAuthService _SpotifyAuthService;

    public CustomSpotifyHttpClient(SpotifyAuthService spotifyAuthService)
    {
        _Client = new HttpClient();
        _SpotifyAuthService = spotifyAuthService;
        Setup();
    }

    private void Setup()
    {
        SpotifyAuth spotifyAuth = _SpotifyAuthService.GetSpotifyAuth();

        spotifyAuth = _SpotifyAuthService.FixAuth(spotifyAuth).Result;
        _Client.DefaultRequestHeaders.Add("client-id", spotifyAuth.ClientId);
        _Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", spotifyAuth.Token);
    }
    
    private async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _Client.GetAsync(url);
    }
    
    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content)
    {
        return await _Client.PutAsync(url, content);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
    {
        return await _Client.SendAsync(message);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content)
    {
        return await _Client.PostAsync(url, content);
    }

    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        try
        {
            HttpResponseMessage response = await _Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] content = await response.Content.ReadAsByteArrayAsync();

            return content;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving data: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> DecipherData<T>(string url) where T : notnull
    {
        HttpResponseMessage message = await GetAsync(url);
        string json = await message.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task<CurrentlyPlaying?> GetCurrentlyPlayingTrack(string url)
    {
        CurrentlyPlaying item = new CurrentlyPlaying();

        HttpResponseMessage response = await GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();
        if(string.IsNullOrEmpty(json))
            return null;
        
        JObject jsonObject = JObject.Parse(json);

        item.CurrentlyPlayingType = jsonObject["currently_playing_type"]?.ToString() ?? "";
        item.IsPlaying = jsonObject["is_playing"]?.ToString() != "False";
        item.ProgressMs = int.Parse(jsonObject["progress_ms"]?.ToString() ?? "0");
        item.Timestamp = long.Parse(jsonObject["timestamp"]?.ToString() ?? "0");
        item.Item = await GetFullTrack(url) ?? new FullTrack();

        var contextData = jsonObject["context"];
        if (contextData == null)
        {
            item.Context = new Context();
        }
        else
        {
            item.Context = new Context
            {
                ExternalUrls =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        contextData["external_urls"]?.ToString() ?? "") ?? new Dictionary<string, string>(),
                Href = contextData["href"]?.ToString() ?? "",
                Type = contextData["type"]?.ToString() ?? "",
                Uri = contextData["uri"]?.ToString() ?? ""
            };
        }

        return item;
    }

    public async Task<FullTrack?> GetFullTrack(string url)
    {
        FullTrack item = new FullTrack();

        HttpResponseMessage response = await GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(json);
        var itemData = jsonObject["item"];

        if (itemData == null)
            return null;
        //Standard properties
        item.Href = itemData["href"]?.ToString() ?? "";
        item.Id = itemData["id"]?.ToString() ?? "";
        item.Name = itemData["name"]?.ToString() ?? "";
        item.PreviewUrl = itemData["preview_url"]?.ToString() ?? "";
        item.Type = itemData["type"]?.ToString() ?? "";
        item.Uri = itemData["uri"]?.ToString() ?? "";
        item.DiscNumber = Convert.ToInt32(itemData["disc_number"]?.ToString());
        item.DurationMs = Convert.ToInt32(itemData["duration_ms"]?.ToString());
        item.TrackNumber = Convert.ToInt32(itemData["track_number"]?.ToString());
        item.Popularity = Convert.ToInt32(itemData["popularity"]?.ToString());

        item.Explicit = itemData["explicit"]?.ToString() != "False";
        item.IsLocal = itemData["is_local"]?.ToString() != "False";

        itemData["available_markets"] = itemData["available_markets"]?.ToString().Replace("[", "").Replace("]", "")
            .Replace("\n", "").Replace("\r", "").Trim();
        string[] elements = itemData["available_markets"]?.ToString().Split(["\",", "\""], StringSplitOptions.RemoveEmptyEntries) ?? [];

        for (int i = 0; i < elements.Length; i++)
        {
            elements[i] = elements[i].Replace("\"", "").Trim();
        }

        item.AvailableMarkets = new List<string>(elements).Where(x => !string.IsNullOrEmpty(x)).ToList();
        item.ExternalIds =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(itemData["external_ids"]?.ToString() ?? "") ?? new Dictionary<string, string>();
        item.ExternalUrls =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(itemData["external_urls"]?.ToString() ?? "") ?? new Dictionary<string, string>();
        item.Artists = JsonConvert.DeserializeObject<List<SimpleArtist>>(itemData["artists"]?.ToString() ?? "") ?? [];

        //Building complex objects
        var albumData = itemData["album"];
        SimpleAlbum album = new SimpleAlbum();
        if(albumData == null)
            item.Album = new SimpleAlbum();
        else
        {
            album = new SimpleAlbum
            {
                AlbumType = albumData["album_type"]?.ToString() ?? "",
                Href = albumData["href"]?.ToString() ?? "",
                Id = albumData["id"]?.ToString() ?? "",
                Name = albumData["name"]?.ToString() ?? "",
                ReleaseDate = albumData["release_date"]?.ToString() ?? "",
                ReleaseDatePrecision = albumData["release_date_precision"]?.ToString() ?? "",
                Type = albumData["type"]?.ToString() ?? "",
                Uri = albumData["uri"]?.ToString() ?? "",
                TotalTracks = Convert.ToInt32(albumData["total_tracks"]?.ToString())
            };

            elements = albumData["available_markets"]?.ToString().Split(["\",", "\""], StringSplitOptions.RemoveEmptyEntries) ?? [];

            for (int i = 0; i < elements.Length; i++)
            {
                elements[i] = elements[i].Replace("\"", "").Trim();
            }

            album.AvailableMarkets = new List<string>(elements).Where(x => !string.IsNullOrEmpty(x)).ToList();
            album.ExternalUrls =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(albumData["external_urls"]?.ToString() ?? "") ?? new Dictionary<string, string>();
            album.Artists = JsonConvert.DeserializeObject<List<SimpleArtist>>(albumData["artists"]?.ToString() ?? "") ?? [];
            album.Images = JsonConvert.DeserializeObject<List<Image>>(albumData["images"]?.ToString() ?? "") ?? [];
        }

        item.Album = album;
        return item;
    }

    public async Task<List<Track>?> InterpretSongSearchResult(string url)
    {
        HttpResponseMessage httpResponse = await GetAsync(url);
        string json = await httpResponse.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(json);
        var trackData = jsonObject["tracks"];
        var itemData = trackData?["items"];
        
        return itemData == null ? [] : JsonConvert.DeserializeObject<List<Track>>(itemData.ToString());
    }
}