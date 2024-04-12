using System.Net;
using Newtonsoft.Json;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Models.Twitch.Auth;
using SpekkieTwitchBot.Twitch.FileHandling;

namespace SpekkieTwitchBot.Auth;

public class TwitchAuthService
{
    private readonly TwitchFileReader _TwitchFileReader;
    private readonly TwitchFileWriter _TwitchFileWriter;
    private readonly Logger _Logger;
    
    public TwitchAuthService(
        TwitchFileReader twitchFileReader,
        TwitchFileWriter twitchFileWriter,
        Logger logger)
    {
        _TwitchFileReader = twitchFileReader;
        _TwitchFileWriter = twitchFileWriter;
        _Logger = logger;
    }
    
    public async Task<TwitchAuth> SetupAuth()
    {
        string jsonData = _TwitchFileReader.ReadTwitchAppAuthFile();
        TwitchAuth auth = JsonConvert.DeserializeObject<TwitchAuth>(jsonData) ?? new TwitchAuth();        
        AuthorizationCredentials authCred = await GetAppAccessAuthCredentials(auth) ?? new AuthorizationCredentials();

        string jsonData2 = _TwitchFileReader.ReadTwitchUserAuthFile();
        TwitchAuth auth2 = JsonConvert.DeserializeObject<TwitchAuth>(jsonData2) ?? new TwitchAuth();
        AuthorizationCredentials authCred2 = await GetUserAccessAuthCredentials(auth2) ?? new AuthorizationCredentials();

        UpdateTwitchSettings(auth, authCred.access_token, authCred2.access_token, authCred.refresh_token, authCred2.refresh_token);
        auth.UserToken = authCred2.access_token;
        auth.AppToken = authCred.access_token;
        return auth;
    }
    
    private async Task<AuthorizationCredentials?> GetAppAccessAuthCredentials(TwitchAuth twitchAuth)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchAuth.ClientSecret),
            new KeyValuePair<string, string>("code", twitchAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                _Logger.LogInfo(responseContent);
                Console.WriteLine($"{responseContent}");
                AuthorizationCredentials? cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshAppAccessTokenAsync(twitchAuth.ClientId, twitchAuth.ClientSecret, twitchAuth.AppRefreshToken);
                return cred;
            default:
                _Logger.LogError($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }
    
    private async Task<AuthorizationCredentials?> GetUserAccessAuthCredentials(TwitchAuth twitchAuth)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchAuth.ClientSecret),
            new KeyValuePair<string, string>("code", twitchAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                _Logger.LogInfo(responseContent);
                Console.WriteLine($"{responseContent}");
                AuthorizationCredentials? cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshAppAccessTokenAsync(twitchAuth.ClientId, twitchAuth.ClientSecret, twitchAuth.UserRefreshToken);
                return cred;
            default:
                _Logger.LogError($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }
    
    private async Task<AuthorizationCredentials?> RefreshAppAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _Logger.LogInfo($"Refreshed token successfully: {responseContent}");
            Console.WriteLine($"{responseContent}");
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        _Logger.LogError($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private void UpdateTwitchSettings(TwitchAuth twitchAuth, string appToken, string userToken, string appRefreshToken, string userRefreshToken)
    {
        twitchAuth.AppToken = appToken;
        twitchAuth.UserToken = userToken;
        twitchAuth.AppRefreshToken = appRefreshToken;
        twitchAuth.UserRefreshToken = userRefreshToken;
        
        string json = JsonConvert.SerializeObject(twitchAuth);
        string json2 = JsonConvert.SerializeObject(twitchAuth);
        
        _TwitchFileWriter.WriteTwitchUserAuthFile(json);
        _TwitchFileWriter.WriteTwitchAppAuthFile(json2);
    }
}