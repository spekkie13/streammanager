using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Twitch.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using AuthorizationCredentials = SpekkieTwitchBot.Models.Twitch.AuthorizationCredentials;

namespace SpekkieTwitchBot.Web;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly CustomTwitchClient _TwitchClient;
    private readonly TwitchPubSub _TwitchPubSub;
    private readonly TwitchAuth _TwitchAuth;
    
    public TwitchWebsocketService(
        IConfiguration configuration, 
        ILogger<TwitchWebsocketService> logger,
        CustomTwitchClient twitchClient, 
        TwitchPubSub twitchPubSub)
    {
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _TwitchAuth = AuthUtils.GetTwitchAuth();
        AuthorizationCredentials authCred = GetAuthorizationCredentials().Result;
        ClientCredentials clientCred = GetClientCredentials().Result;
        _TwitchAuth.AppToken = authCred.access_token;
        _TwitchAuth.UserToken = clientCred.access_token;
        
        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        ConnectionCredentials cred = new ConnectionCredentials("spekkie1313", _TwitchAuth.Implicit_OAuth);
        _TwitchClient.Initialize(cred, _TwitchAuth.BroadcasterName);
        _TwitchClient.OnCommunitySubscription += OnCommunitySubscription;
        _TwitchClient.OnNewSubscriber += OnNewSubscriber;
        _TwitchClient.OnGiftedSubscription += OnGiftedSubscription;
        _TwitchClient.OnReSubscriber += OnReSubscriber;
        _TwitchClient.OnContinuedGiftedSubscription += OnContinuedGiftSubscription;
        _TwitchClient.OnPrimePaidSubscriber += OnPrimePaidSubscriber;
        
        _TwitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));
        SetupPubSub();
    }

    private void SetupPubSub()
    {
        _TwitchPubSub.OnPubSubServiceConnected += OnPubSubConnected;
        _TwitchPubSub.OnListenResponse += OnListenResponse;
        _TwitchPubSub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        
        _TwitchPubSub.ListenToVideoPlayback(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToFollows(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToSubscriptions(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToLeaderboards(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToPredictions(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToRaid(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToChannelPoints(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToBitsEventsV2(_TwitchAuth.ChannelId);
        
    }

    private void OnPubSubConnected(object? sender, EventArgs e)
    {
        _TwitchPubSub.SendTopics(_TwitchAuth.AppToken);
        _Logger.LogInformation("Connected");
    }

    private void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        Console.WriteLine(e.Successful
            ? $"Successfully listening to: {e.Topic}"
            : $"Failed to listen! Error: {e.Response.Error}");
    }

    private void OnChannelPointsRewardRedeemed(object? sender, OnChannelPointsRewardRedeemedArgs e)
    {
        Console.WriteLine("Channel Points redeemed");
        Console.WriteLine($"{e.RewardRedeemed.Redemption.Id}");
    }
    
    private void OnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.GiftedSubscription.DisplayName}");
    }
    
    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.Subscriber}");
    }
    
    private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.GiftedSubscription.DisplayName}");
    }
    
    private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.ReSubscriber.DisplayName}");
    }
    
    private void OnContinuedGiftSubscription(object? sender, OnContinuedGiftedSubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.ContinuedGiftedSubscription.DisplayName}");
    }
    
    private void OnPrimePaidSubscriber(object? sender, OnPrimePaidSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.PrimePaidSubscriber.DisplayName}");
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _TwitchClient.Connect();
        _TwitchPubSub.Connect();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _TwitchClient.Disconnect();
        _TwitchPubSub.Disconnect();
        return Task.CompletedTask;
    }
    
    private async Task<ClientCredentials> GetClientCredentials()
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _TwitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", _TwitchAuth.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            ClientCredentials cred = JsonConvert.DeserializeObject<ClientCredentials>(responseContent);
            return cred;
        }
        
        Console.WriteLine($"Failed to get access token. Status code: {response.StatusCode}");
        return null;
    }

    private async Task<AuthorizationCredentials> GetAuthorizationCredentials()
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _TwitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", _TwitchAuth.ClientSecret),
            new KeyValuePair<string, string>("code", _TwitchAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                AuthorizationCredentials cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                UpdateTwitchSettings(cred);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshTokenAsync(_TwitchAuth.ClientId, _TwitchAuth.ClientSecret, _TwitchAuth.RefreshToken);
                return cred;
            default:
                Console.WriteLine($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }
    
    async Task<AuthorizationCredentials> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
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
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        // Handle error
        Console.WriteLine($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private void UpdateTwitchSettings(AuthorizationCredentials? cred)
    {
        _TwitchAuth.RefreshToken = cred?.refresh_token ?? "";
        string json = JsonConvert.SerializeObject(_TwitchAuth);
        FileHandler.WriteTwitchAuthFile(json);
    }
}