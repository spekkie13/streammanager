using System.Text.Json;
using System.Text.Json.Serialization;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.Systems.StreamStats;

public class StreamStatsClient
{
    private readonly HttpClient _Http = new();
    private readonly string _ConfigPath = Path.Combine(BotPaths.BaseDir, "Settings", "streamstats.json");

    private record StreamStatsConfig(
        [property: JsonPropertyName("apiUrl")] string ApiUrl,
        [property: JsonPropertyName("apiKey")] string ApiKey
    );

    private record SubEvent(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("giftCount")] int? GiftCount
    );

    private record SubsResponse(
        [property: JsonPropertyName("events")] List<SubEvent> Events
    );

    public async Task<int?> GetSubCountAsync(CancellationToken ct = default)
    {
        StreamStatsConfig? config = ReadConfig();
        if (config == null) return null;

        try
        {
            string url = $"{config.ApiUrl.TrimEnd('/')}/api/subs";
            HttpRequestMessage req = new(HttpMethod.Get, url);
            req.Headers.Add("x-api-key", config.ApiKey);

            using HttpResponseMessage response = await _Http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[StreamStats] API returned {(int)response.StatusCode}");
                return null;
            }

            string body = await response.Content.ReadAsStringAsync(ct);
            SubsResponse? result = JsonSerializer.Deserialize<SubsResponse>(body);
            if (result == null) return null;

            int total = result.Events.Sum(e =>
                e.Kind == "community_gift" ? (e.GiftCount ?? 1) : 1);

            Console.WriteLine($"[StreamStats] Fetched {result.Events.Count} events, total subs: {total}");
            return total;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StreamStats] Error fetching sub count: {ex.Message}");
            return null;
        }
    }

    private StreamStatsConfig? ReadConfig()
    {
        if (!File.Exists(_ConfigPath)) return null;

        try
        {
            string json = File.ReadAllText(_ConfigPath);
            StreamStatsConfig? config = JsonSerializer.Deserialize<StreamStatsConfig>(json);
            if (string.IsNullOrWhiteSpace(config?.ApiUrl) || string.IsNullOrWhiteSpace(config.ApiKey))
                return null;

            return config;
        }
        catch
        {
            return null;
        }
    }
}
