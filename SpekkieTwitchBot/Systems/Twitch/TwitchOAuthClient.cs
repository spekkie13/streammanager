using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchOAuthClient
{
    private readonly HttpClient _Http;
    private readonly Logger _Logger;
    
    public TwitchOAuthClient(HttpClient client, Logger log)
    {
        _Http = client;
        _Logger = log;
    }
    
    public async Task<AuthorizationCredentials> RefreshUserTokenAsync(
        string clientId,
        string clientSecret,
        string refreshToken,
        CancellationToken ct)
    {
        var parameters = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        ]);

        var response = await _Http.PostAsync("https://id.twitch.tv/oauth2/token", parameters, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Refresh failed: {response.StatusCode} - {body}");

        _Logger.LogInfo("Twitch token refreshed");
        return JsonConvert.DeserializeObject<AuthorizationCredentials>(body)!;
    }
}