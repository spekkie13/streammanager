using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieTwitchBot.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Web;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly TwitchClient _TwitchClient;
    private readonly TwitchPubSub _TwitchPubSub;
    private readonly string _Token;
    private readonly string _ChannelId;
    
    public TwitchWebsocketService(IConfiguration configuration, ILogger<TwitchWebsocketService> logger,
        TwitchClient twitchClient, TwitchPubSub twitchPubSub)
    {
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var twitchAuth = AuthUtils.GetTwitchAuth();
        _ChannelId = twitchAuth.ChannelId;
        _Token = twitchAuth.OAuth;
        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        ConnectionCredentials cred = new ConnectionCredentials("spekkie1313", _Token);
        _TwitchClient.Initialize(cred, twitchAuth.BroadcasterName);
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
        
        _TwitchPubSub.ListenToVideoPlayback(_ChannelId);
        _TwitchPubSub.ListenToFollows(_ChannelId);
        _TwitchPubSub.ListenToSubscriptions(_ChannelId);
        _TwitchPubSub.ListenToLeaderboards(_ChannelId);
        _TwitchPubSub.ListenToPredictions(_ChannelId);
        _TwitchPubSub.ListenToRaid(_ChannelId);
        _TwitchPubSub.ListenToChannelPoints(_ChannelId);
        _TwitchPubSub.ListenToBitsEventsV2(_ChannelId);
    }

    private void OnPubSubConnected(object? sender, EventArgs e)
    {
        _TwitchPubSub.SendTopics(_Token);
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
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _TwitchClient.Disconnect();
        return Task.CompletedTask;
    }
}