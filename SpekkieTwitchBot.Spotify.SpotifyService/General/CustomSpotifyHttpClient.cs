using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;

namespace SpotifyAuthService.General;

public class CustomSpotifyHttpClient
{
    private readonly HttpClient _Client;
    private readonly Logger _Logger;
    private readonly Auth.SpotifyAuthService _SpotifyAuthService;

    public CustomSpotifyHttpClient(Auth.SpotifyAuthService spotifyAuthService, Logger logger)
    {
        _Client = new HttpClient();
        _Logger = logger;
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
        HttpResponseMessage response = await _Client.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        Setup();
        response = await _Client.GetAsync(url);
        return response;
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
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Setup();
                response = await _Client.GetAsync(url);
            }
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            return content;
        }
        catch (HttpRequestException ex)
        {
            _Logger.LogError($"Error occured retrieving data: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> DecipherData<T>(string url) where T : notnull
    {
        HttpResponseMessage message = await GetAsync(url);
        if (message.StatusCode == HttpStatusCode.Unauthorized)
        {
            Setup();
            message = await _Client.GetAsync(url);
        }
        string json = await message.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task<CurrentlyPlaying?> GetCurrentlyPlayingTrack(string url)
    {
        CurrentlyPlaying item = new ();

        HttpResponseMessage response = await GetAsync(url);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Setup();
            response = await GetAsync(url);
        }
        
        string json = await response.Content.ReadAsStringAsync();
        if(string.IsNullOrEmpty(json))
            return null;
        
        JObject jsonObject = JObject.Parse(json);

        item.CurrentlyPlayingType = jsonObject["currently_playing_type"]?.ToString() ?? "";
        item.IsPlaying = jsonObject["is_playing"]?.ToString() != "False";
        item.ProgressMs = int.Parse(jsonObject["progress_ms"]?.ToString() ?? "0");
        item.Timestamp = long.Parse(jsonObject["timestamp"]?.ToString() ?? "0");
        item.Item = await GetFullTrack(url) ?? new FullTrack();

        JToken? contextData = jsonObject["context"];
        if (contextData == null)
        {
            item.Context = new Context();
        }
        else
        {
            JToken? urls = contextData["external_urls"];
            Context? context = JsonConvert.DeserializeObject<Context>(json);
            if(context == null)
                context = new Context();
            else
                context.ExternalUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(urls?.ToString() ?? string.Empty) ?? new Dictionary<string, string>();
            item.Context = context;
        }
        
        return item;
    }

    public async Task<FullTrack?> GetFullTrack(string url)
    {
        HttpResponseMessage response = await GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Setup(); // refresh token etc.
            response = await GetAsync(url);
        }

        // 204/empty = normaal
        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
            return null;

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch
        {
            return null; // slechte/lege JSON -> stil
        }

        // item kan null zijn (ads/podcasts/device switch)
        var itemData = root["item"] as JObject;
        if (itemData is null)
            return null;

        var item = new FullTrack
        {
            Href = itemData.Value<string>("href") ?? "",
            Id = itemData.Value<string>("id") ?? "",
            Name = itemData.Value<string>("name") ?? "",
            PreviewUrl = itemData.Value<string>("preview_url") ?? "",
            Type = itemData.Value<string>("type") ?? "",
            Uri = itemData.Value<string>("uri") ?? "",
            DiscNumber = itemData.Value<int?>("disc_number") ?? 0,
            DurationMs = itemData.Value<int?>("duration_ms") ?? 0,
            TrackNumber = itemData.Value<int?>("track_number") ?? 0,
            Popularity = itemData.Value<int?>("popularity") ?? 0,
            Explicit = itemData.Value<bool?>("explicit") ?? false,
            IsLocal = itemData.Value<bool?>("is_local") ?? false,
        };

        // --- available_markets SAFE ---
        item.AvailableMarkets = ReadStringArray(itemData["available_markets"]);

        // Dictionaries/lists veilig deserializen
        item.ExternalIds = SafeDeserialize<Dictionary<string, string>>(itemData["external_ids"]) ?? new();
        item.ExternalUrls = SafeDeserialize<Dictionary<string, string>>(itemData["external_urls"]) ?? new();
        item.Artists = SafeDeserialize<List<SimpleArtist>>(itemData["artists"]) ?? new();

        // --- Album ---
        var albumData = itemData["album"] as JObject;
        if (albumData is null)
        {
            item.Album = new SimpleAlbum();
            return item;
        }

        var album = new SimpleAlbum
        {
            AlbumType = albumData.Value<string>("album_type") ?? "",
            Href = albumData.Value<string>("href") ?? "",
            Id = albumData.Value<string>("id") ?? "",
            Name = albumData.Value<string>("name") ?? "",
            ReleaseDate = albumData.Value<string>("release_date") ?? "",
            ReleaseDatePrecision = albumData.Value<string>("release_date_precision") ?? "",
            Type = albumData.Value<string>("type") ?? "",
            Uri = albumData.Value<string>("uri") ?? "",
            TotalTracks = albumData.Value<int?>("total_tracks") ?? 0,
            AvailableMarkets = ReadStringArray(albumData["available_markets"]),
            ExternalUrls = SafeDeserialize<Dictionary<string, string>>(albumData["external_urls"]) ?? new(),
            Artists = SafeDeserialize<List<SimpleArtist>>(albumData["artists"]) ?? new(),
            Images = SafeDeserialize<List<Image>>(albumData["images"]) ?? new(),
        };

        item.Album = album;
        return item;
    }

    private static List<string> ReadStringArray(JToken? token)
    {
        // Spotify: meestal JArray, maar soms null / inconsistent
        if (token is JArray arr)
            return arr.Values<string>().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        // als het tóch als string binnenkomt (edge case), supporten we dat ook
        if (token?.Type != JTokenType.String) return [];
        {
            var s = token.Value<string>() ?? "";
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().Trim('"'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

    }

    private static T? SafeDeserialize<T>(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
            return default;

        try
        {
            // token.ToString() is ok, maar JToken.ToObject is cleaner:
            return token.ToObject<T>();
        }
        catch
        {
            return default;
        }
    }


    public async Task<List<Track>?> InterpretSongSearchResult(string url)
    {
        HttpResponseMessage httpResponse = await GetAsync(url);
        if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            Setup();
            httpResponse = await GetAsync(url);
        }
        string json = await httpResponse.Content.ReadAsStringAsync();
        JObject jsonObject = JObject.Parse(json);
        JToken? trackData = jsonObject["tracks"];
        JToken? itemData = trackData?["items"];
        
        return itemData == null ? [] : JsonConvert.DeserializeObject<List<Track>>(itemData.ToString());
    }
}