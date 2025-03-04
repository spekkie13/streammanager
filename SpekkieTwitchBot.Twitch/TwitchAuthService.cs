#nullable disable
using System.Net;
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;

namespace TwitchAuthService;

public class TwitchAuthService(TwitchFileReader twitchFileReader, TwitchFileWriter twitchFileWriter, Logger logger)
{
    private GeneralTwitchAuth _twitchGeneralAuth;
    private TwitchUserAuth _twitchUserAuth;

    public TwitchUserAuth GetTwitchUserAuth()
    {
        string jsonData = twitchFileReader.ReadTwitchUserAuthFile();
        _twitchUserAuth = JsonConvert.DeserializeObject<TwitchUserAuth>(jsonData) ?? new TwitchUserAuth();
        AuthorizationCredentials authCred = GetUserAccessAuthCredentials(_twitchUserAuth).Result;

        UpdateTwitchSettings(_twitchUserAuth, authCred);

        return _twitchUserAuth;
    }

    public GeneralTwitchAuth GetGeneralTwitchAuth()
    {
        string jsonData = twitchFileReader.ReadTwitchGeneralAuthFile();
        _twitchGeneralAuth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData) ?? new GeneralTwitchAuth();
        return _twitchGeneralAuth;
    }

    private async Task<AuthorizationCredentials> GetUserAccessAuthCredentials(TwitchUserAuth twitchUserAuth)
    {
        using HttpClient client = new HttpClient();

        FormUrlEncodedContent parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", twitchUserAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", twitchUserAuth.ClientSecret),
            new KeyValuePair<string, string>("code", twitchUserAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        HttpResponseMessage response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                string responseContent = await response.Content.ReadAsStringAsync();
                logger.LogInfo(responseContent);
                AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent) ??
                                                new AuthorizationCredentials();
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshAppAccessTokenAsync(_twitchUserAuth.ClientId, _twitchUserAuth.ClientSecret,
                    _twitchUserAuth.UserRefreshToken);
                return cred;
            default:
                logger.LogError($"Failed to get tokens. Status code: {response.StatusCode}");
                return new AuthorizationCredentials();
        }
    }

    private async Task<AuthorizationCredentials> RefreshAppAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        using HttpClient client = new HttpClient();
        FormUrlEncodedContent parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        HttpResponseMessage response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInfo($"Refreshed token successfully: {responseContent}");
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        logger.LogError($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private void UpdateTwitchSettings(TwitchUserAuth twitchUserAuth, AuthorizationCredentials authCred)
    {
        twitchUserAuth.UserToken = authCred.AccessToken;
        twitchUserAuth.UserRefreshToken = authCred.RefreshToken;

        string json = JsonConvert.SerializeObject(twitchUserAuth);
        twitchFileWriter.WriteTwitchUserAuthFile(json);
    }
}