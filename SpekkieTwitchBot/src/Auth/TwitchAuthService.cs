using System.Net;
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Twitch.FileHandling;

namespace SpekkieTwitchBot.Auth;

public class TwitchAuthService
{
    private TwitchUserAuth? _twitchUserAuth;
    private GeneralTwitchAuth? _twitchGeneralAuth;

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

    public TwitchUserAuth GetTwitchUserAuth()
    {
        string jsonData = _TwitchFileReader.ReadTwitchUserAuthFile();
        _twitchUserAuth = JsonConvert.DeserializeObject<TwitchUserAuth>(jsonData) ?? new TwitchUserAuth();
        AuthorizationCredentials authCred =
            GetUserAccessAuthCredentials(_twitchUserAuth).Result ?? new AuthorizationCredentials();

        UpdateTwitchSettings(_twitchUserAuth, authCred);

        return _twitchUserAuth;
    }

    public GeneralTwitchAuth GetGeneralTwitchAuth()
    {
        string jsonData = _TwitchFileReader.ReadTwitchGeneralAuthFile();
        _twitchGeneralAuth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData) ?? new GeneralTwitchAuth();
        return _twitchGeneralAuth;
    }

    private async Task<AuthorizationCredentials> GetUserAccessAuthCredentials(TwitchUserAuth twitchUserAuth)
    {
        using HttpClient client = new HttpClient();

        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchUserAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchUserAuth.ClientSecret),
            new KeyValuePair<string, string>("code", twitchUserAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                _Logger.LogInfo(responseContent);
                AuthorizationCredentials cred =
                    JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshAppAccessTokenAsync(_twitchUserAuth.ClientId, _twitchUserAuth.ClientSecret,
                    _twitchUserAuth.UserRefreshToken);
                return cred;
            default:
                _Logger.LogError($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }

    private async Task<AuthorizationCredentials> RefreshAppAccessTokenAsync(string clientId, string clientSecret,
        string refreshToken)
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
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        _Logger.LogError($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private void UpdateTwitchSettings(TwitchUserAuth twitchUserAuth, AuthorizationCredentials authCred)
    {
        twitchUserAuth.UserToken = authCred.AccessToken;
        twitchUserAuth.UserRefreshToken = authCred.RefreshToken;

        string json = JsonConvert.SerializeObject(twitchUserAuth);
        _TwitchFileWriter.WriteTwitchUserAuthFile(json);
    }
}