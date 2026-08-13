using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieClassLibrary.Spotify.Song;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Spotify;

namespace SpotifyAuthService.General;

public sealed class CustomSpotifyHttpClient
{
    private readonly HttpClient _Client;
    private readonly Logger _Logger;
    private readonly Auth.SpotifyAuthService _SpotifyAuthService;

    // After a failed auth (revoked refresh token, bad secret) every caller used to re-run the full
    // load+refresh — once per 5-second overlay tick — spamming the log and burning a Spotify token
    // request each time. Failures are cached and retried at most once per cooldown instead.
    private static readonly TimeSpan AuthRetryCooldown = TimeSpan.FromMinutes(5);

    private const string ReauthScript = "SpekkieTwitchBot.Spotify.SpotifyService/Tools/Reauth-Spotify.ps1";

    private readonly SemaphoreSlim _SetupLock = new(1, 1);
    private volatile bool _IsConfigured;
    private DateTime _NextAuthAttemptUtc = DateTime.MinValue;
    private string? _LastAuthError;

    // A revoked refresh token is not a transient failure — retrying on a timer can never fix it.
    // Instead of backing off forever, stop attempting and wait for Spotify.json to actually change
    // on disk, which is exactly what the re-auth script does.
    private bool _ReauthRequired;
    private DateTime _AuthFileStampAtFailure;

    public CustomSpotifyHttpClient(HttpClient client, Auth.SpotifyAuthService spotifyAuthService, Logger logger)
    {
        _Client = client;
        _Logger = logger;
        _SpotifyAuthService = spotifyAuthService;
    }

    private async Task EnsureConfiguredAsync(CancellationToken ct)
    {
        if (_IsConfigured) return;

        await _SetupLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_IsConfigured) return;

            // Waiting on a human to re-authorize: no point issuing token requests until the
            // credentials file is rewritten, but pick it up automatically once it is.
            if (_ReauthRequired)
            {
                if (SpotifyFileReader.GetSpotifyAuthLastWriteUtc() == _AuthFileStampAtFailure)
                    throw new Auth.SpotifyAuthException(
                        _LastAuthError ?? "Spotify re-authorization required", true);

                _Logger.LogInfo("[SPOTIFY] Spotify.json changed — retrying authorization.");
                _ReauthRequired = false;
            }

            // Still inside the cooldown from a previous transient failure — fail fast and silently.
            if (DateTime.UtcNow < _NextAuthAttemptUtc)
                throw new Auth.SpotifyAuthException(_LastAuthError ?? "Spotify auth unavailable");

            _Logger.LogInfo("[SPOTIFY] EnsureConfigured START");

            try
            {
                SpotifyAuth spotifyAuth = _SpotifyAuthService.GetSpotifyAuth();

                spotifyAuth = await _SpotifyAuthService.FixAuth(spotifyAuth).ConfigureAwait(false);

                // headers safe zetten (remove/replace)
                if (_Client.DefaultRequestHeaders.Contains("client-id"))
                    _Client.DefaultRequestHeaders.Remove("client-id");

                _Client.DefaultRequestHeaders.Add("client-id", spotifyAuth.ClientId);
                _Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", spotifyAuth.Token);

                _IsConfigured = true;
                _NextAuthAttemptUtc = DateTime.MinValue;
                _LastAuthError = null;
                _ReauthRequired = false;

                _Logger.LogInfo("[SPOTIFY] EnsureConfigured END");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _LastAuthError = ex.Message;

                if (ex is Auth.SpotifyAuthException { RequiresReauthorization: true })
                {
                    _ReauthRequired = true;
                    _AuthFileStampAtFailure = SpotifyFileReader.GetSpotifyAuthLastWriteUtc();
                    _Logger.LogError(
                        "[SPOTIFY] Re-authorization required — the refresh token is no longer valid and retrying " +
                        $"cannot fix it. Run {ReauthScript}; the new token is picked up automatically, no restart " +
                        "needed. Spotify polling is paused until then.");
                }
                else
                {
                    _NextAuthAttemptUtc = DateTime.UtcNow.Add(AuthRetryCooldown);
                    _Logger.LogError(
                        $"[SPOTIFY] Auth failed, retrying in {AuthRetryCooldown.TotalMinutes:0} min: {ex.Message}");
                }

                throw;
            }
        }
        finally
        {
            _SetupLock.Release();
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
    {
        await EnsureConfiguredAsync(ct).ConfigureAwait(false);

        HttpResponseMessage response = await _Client.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // token expired → reconfigure + retry
        _IsConfigured = false;
        await EnsureConfiguredAsync(ct).ConfigureAwait(false);

        return await _Client.GetAsync(url, ct).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct).ConfigureAwait(false);
        return await _Client.PutAsync(url, content, ct).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct).ConfigureAwait(false);
        return await _Client.PostAsync(url, content, ct).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct).ConfigureAwait(false);
        return await _Client.SendAsync(message, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> GetByteArrayAsync(string url, CancellationToken ct = default)
    {
        try
        {
            HttpResponseMessage response = await GetAsync(url, ct).ConfigureAwait(false);
            return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _Logger.LogError($"Error occured retrieving data: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> DecipherData<T>(string url, CancellationToken ct = default) where T : notnull
    {
        HttpResponseMessage message = await GetAsync(url, ct).ConfigureAwait(false);
        string json = await message.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task<CurrentlyPlaying?> GetCurrentlyPlayingTrack(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await GetAsync(url, ct).ConfigureAwait(false);

        string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
            return null;

        JObject jsonObject = JObject.Parse(json);

        CurrentlyPlaying item = new CurrentlyPlaying();
        item.CurrentlyPlayingType = jsonObject["currently_playing_type"]?.ToString() ?? "";
        item.IsPlaying = jsonObject["is_playing"]?.ToString() != "False";
        item.ProgressMs = int.Parse(jsonObject["progress_ms"]?.ToString() ?? "0");
        item.Timestamp = long.Parse(jsonObject["timestamp"]?.ToString() ?? "0");
        item.Item = await GetFullTrack(url, ct).ConfigureAwait(false) ?? new FullTrack();

        JToken? contextData = jsonObject["context"];
        if (contextData == null)
        {
            item.Context = new Context();
        }
        else
        {
            JToken? urls = contextData["external_urls"];
            Context? context = JsonConvert.DeserializeObject<Context>(json);
            context ??= new Context();

            context.ExternalUrls =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(urls?.ToString() ?? string.Empty)
                ?? new Dictionary<string, string>();

            item.Context = context;
        }

        return item;
    }

    public async Task<FullTrack?> GetFullTrack(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
            return null;

        JObject root;
        try { root = JObject.Parse(json); }
        catch { return null; }

        if (root["item"] is not JObject itemData)
            return null;

        FullTrack item = new FullTrack
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
            AvailableMarkets = ReadStringArray(itemData["available_markets"]),
            ExternalIds = SafeDeserialize<Dictionary<string, string>>(itemData["external_ids"]) ?? new(),
            ExternalUrls = SafeDeserialize<Dictionary<string, string>>(itemData["external_urls"]) ?? new(),
            Artists = SafeDeserialize<List<SimpleArtist>>(itemData["artists"]) ?? []
        };

        if (itemData["album"] is not JObject albumData)
        {
            item.Album = new SimpleAlbum();
            return item;
        }

        item.Album = new SimpleAlbum
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
            Artists = SafeDeserialize<List<SimpleArtist>>(albumData["artists"]) ?? [],
            Images = SafeDeserialize<List<Image>>(albumData["images"]) ?? [],
        };

        return item;
    }

    public async Task<string> GetTrackDisplayNameAsync(string trackId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await GetAsync($"https://api.spotify.com/v1/tracks/{trackId}", ct).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return "";

        JObject root;
        try { root = JObject.Parse(json); }
        catch { return ""; }

        string name = root["name"]?.ToString() ?? "";
        if (root["artists"] is not JArray artistsArray) return name;

        string artists = string.Join(" & ", artistsArray
            .Select(a => a["name"]?.ToString())
            .Where(n => !string.IsNullOrWhiteSpace(n)));

        return string.IsNullOrWhiteSpace(artists) ? name : $"{name} by {artists}";
    }

    public async Task<List<Track>?> InterpretSongSearchResult(string url, CancellationToken ct = default)
    {
        HttpResponseMessage httpResponse = await GetAsync(url, ct).ConfigureAwait(false);
        string json = await httpResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        JObject jsonObject = JObject.Parse(json);

        JToken? itemData = jsonObject["tracks"]?["items"];
        return itemData == null ? [] : JsonConvert.DeserializeObject<List<Track>>(itemData.ToString());
    }

    private static List<string> ReadStringArray(JToken? token)
    {
        if (token is JArray arr)
            return arr.Values<string>().Where(s => !string.IsNullOrWhiteSpace(s)).ToList()!;

        if (token?.Type != JTokenType.String) return [];

        string s = token.Value<string>() ?? "";
        return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().Trim('"'))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static T? SafeDeserialize<T>(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
            return default;

        try { return token.ToObject<T>(); }
        catch { return default; }
    }
}