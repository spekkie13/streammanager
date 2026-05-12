using Newtonsoft.Json;
using SpekkieClassLibrary.ClashOfClans.Ccn;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class CcnHttpClient(Logger logger)
{
    private readonly HttpClient _HttpClient = new();

    private const string BaseUrl = "https://api.competitiveclash.network";

    public async Task<CcnClanInfo?> GetClanInfoAsync(string clanTag, CancellationToken ct = default)
    {
        try
        {
            string encoded = clanTag.Replace("#", "%23");
            string url = $"{BaseUrl}/clans/info?clan={encoded}";

            HttpResponseMessage response = await _HttpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"[CCN] Clan info request failed for '{clanTag}': {(int)response.StatusCode}");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<CcnClanInfo>(json);
        }
        catch (Exception e)
        {
            logger.LogWarning($"[CCN] GetClanInfoAsync failed for '{clanTag}': {e.Message}");
            return null;
        }
    }
}