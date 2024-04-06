using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Twitch.Client;
using SpekkieTwitchBot.Twitch.Commands;
using SpekkieTwitchBot.Twitch.Pubsub;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using AuthorizationCredentials = SpekkieTwitchBot.Models.Twitch.AuthorizationCredentials;
using OnChannelPointsRewardRedeemedArgs = SpekkieTwitchBot.Models.Twitch.Pubsub.Args.OnChannelPointsRewardRedeemedArgs;

namespace SpekkieTwitchBot.Web;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly CustomTwitchClient _TwitchClient;
    private readonly CustomPubsub _TwitchPubSub;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    private readonly GeneralCommandHandler _GeneralCommandHandler;
    private readonly TwitchAuth _TwitchAuth;

    public TwitchWebsocketService(
        IConfiguration configuration, 
        ILogger<TwitchWebsocketService> logger,
        CustomTwitchClient twitchClient, 
        CustomPubsub twitchPubSub,
        SpotifyCommandHandler spotifyCommandHandler, 
        GeneralCommandHandler generalCommandHandler)
    {
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _SpotifyCommandHandler = spotifyCommandHandler;
        _GeneralCommandHandler = generalCommandHandler;

        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        _TwitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));

        _TwitchAuth = SetupAuth();
        SetupTwitchClient();
        SetupPubSub();
    }

    private static TwitchAuth SetupAuth()
    {
        TwitchAuth twitchAuth = AuthService.GetTwitchAuth();
        AuthorizationCredentials authCred = AuthService.GetAuthorizationCredentials(twitchAuth).Result ?? new AuthorizationCredentials();
        ClientCredentials clientCred = AuthService.GetClientCredentials(twitchAuth).Result ?? new ClientCredentials();
        twitchAuth.AppToken = authCred.access_token;
        twitchAuth.UserToken = clientCred.access_token;

        return twitchAuth;
    }

    private void SetupTwitchClient()
    {
        ConnectionCredentials cred = new ConnectionCredentials("spekkie1313", _TwitchAuth.Implicit_OAuth);
        _TwitchClient.Initialize(cred, _TwitchAuth.BroadcasterName);
        _TwitchClient.OnCommunitySubscription += OnCommunitySubscription;
        _TwitchClient.OnNewSubscriber += OnNewSubscriber;
        _TwitchClient.OnGiftedSubscription += OnGiftedSubscription;
        _TwitchClient.OnReSubscriber += OnReSubscriber;
        _TwitchClient.OnContinuedGiftedSubscription += OnContinuedGiftSubscription;
        _TwitchClient.OnPrimePaidSubscriber += OnPrimePaidSubscriber;
        _TwitchClient.OnChatCommandReceived += OnChatCommandReceived;
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
        Redemption reward = e.RewardRedeemed.Redemption;
        switch (e.RewardRedeemed.Redemption.Reward.Title)
        {
            case "Song Request":
                bool success = _SpotifyCommandHandler.HandleAddSongToQueueCommand(reward.UserInput);
                //string status = success ? "FULFILLED" : "REJECTED";
                // UpdateRedemption(id: e.RewardRedeemed.Redemption.Id, 
                //       broadcasterId: _TwitchAuth.ChannelId,
                //            rewardId: e.RewardRedeemed.Redemption.Reward.Id, 
                //              status: status);
                break;
        }
        Console.WriteLine($"Redeemed: {e.RewardRedeemed.Redemption.Reward.Title}");
    }
    
    private void OnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs e)
    {       
        Console.WriteLine($"{e.GiftedSubscription.DisplayName} just subscribed");
    }
    
    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {       
        Console.WriteLine($"{e.Subscriber} just subscribed");
    }
    
    private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
    {       
        Console.WriteLine($"{e.GiftedSubscription.DisplayName} just gifted a sub");
    }
    
    private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
    {       
        Console.WriteLine($"{e.ReSubscriber.DisplayName} just subscribed for {e.ReSubscriber.Months} Months");
    }
    
    private void OnContinuedGiftSubscription(object? sender, OnContinuedGiftedSubscriptionArgs e)
    {       
        Console.WriteLine($"{e.ContinuedGiftedSubscription.DisplayName} just continued their gifted subscription");
    }
    
    private void OnPrimePaidSubscriber(object? sender, OnPrimePaidSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.PrimePaidSubscriber.DisplayName}");
    }

    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        _GeneralCommandHandler.HandleCommand(e.Command);
    }

    private async void UpdateRedemption(string id, string broadcasterId, string rewardId, string status)
    {
        const string Url = "https://api.twitch.tv/helix/channel_points/custom_rewards/redemptions";
        
        using HttpClient client = new HttpClient(); 
        client.DefaultRequestHeaders.Add("Client-Id", _TwitchAuth.ClientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{_TwitchAuth.AppToken}");
            
        var requestContent = new StringContent($"{{\"status\":\"{status}\"}}", 
            Encoding.UTF8, 
            "application/json");
        
        string requestUrl = $"{Url}?broadcaster_id={broadcasterId}&reward_id={rewardId}&id={id}";
        
        HttpResponseMessage message = await client.PatchAsync(requestUrl, requestContent);
            
        Console.WriteLine(message);
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
}