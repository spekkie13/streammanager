using System.Net;
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;

namespace TwitchAuthService;

public class TwitchAuthService(TwitchFileReader twitchFileReader, TwitchFileWriter twitchFileWriter, Logger logger)
{
    private GeneralTwitchAuth _TwitchGeneralAuth = GeneralTwitchAuth.Empty;
    private TwitchUserAuth _TwitchUserAuth = TwitchUserAuth.Empty;

    public TwitchUserAuth GetTwitchUserAuth()
    {
        string jsonData = twitchFileReader.ReadTwitchUserAuthFile();
        _TwitchUserAuth = JsonConvert.DeserializeObject<TwitchUserAuth>(jsonData) ?? new TwitchUserAuth();
        AuthorizationCredentials authCred = GetUserAccessAuthCredentials(_TwitchUserAuth).Result;

        UpdateTwitchSettings(_TwitchUserAuth, authCred);

        return _TwitchUserAuth;
    }

    public GeneralTwitchAuth GetGeneralTwitchAuth()
    {
        string jsonData = twitchFileReader.ReadTwitchGeneralAuthFile();
        _TwitchGeneralAuth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData) ?? new GeneralTwitchAuth();
        return _TwitchGeneralAuth;
    }

    private async Task<AuthorizationCredentials> GetUserAccessAuthCredentials(TwitchUserAuth twitchUserAuth)
    {
        using HttpClient client = new ();
        
        FormUrlEncodedContent parameters = new ([
            new KeyValuePair<string, string>("client_id", twitchUserAuth.ClientId ?? ""),
            new KeyValuePair<string, string>("client_secret", twitchUserAuth.ClientSecret ?? ""),
            new KeyValuePair<string, string>("code", twitchUserAuth.Code ?? ""),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        ]);

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
                cred = await RefreshAppAccessTokenAsync(_TwitchUserAuth.ClientId, _TwitchUserAuth.ClientSecret,
                    _TwitchUserAuth.UserRefreshToken);
                return cred;
            default:
                logger.LogError($"Failed to get tokens. Status code: {response.StatusCode}");
                return new AuthorizationCredentials();
        }
    }

    private async Task<AuthorizationCredentials> RefreshAppAccessTokenAsync(string? clientId, string? clientSecret, string? refreshToken)
    {
        using HttpClient client = new ();
        FormUrlEncodedContent parameters = new ([
            new KeyValuePair<string, string>("client_id", clientId ?? ""),
            new KeyValuePair<string, string>("client_secret", clientSecret ?? ""),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken ?? "")
        ]);

        HttpResponseMessage response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInfo($"Refreshed token successfully: {responseContent}");
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent) ?? AuthorizationCredentials.Empty;
        }

        logger.LogError($"Error refreshing token: {response.StatusCode}");
        return AuthorizationCredentials.Empty;
    }

    private void UpdateTwitchSettings(TwitchUserAuth twitchUserAuth, AuthorizationCredentials authCred)
    {
        twitchUserAuth.UserToken = authCred.AccessToken;
        twitchUserAuth.UserRefreshToken = authCred.RefreshToken;

        string json = JsonConvert.SerializeObject(twitchUserAuth);
        twitchFileWriter.WriteTwitchUserAuthFile(json);
    }
}