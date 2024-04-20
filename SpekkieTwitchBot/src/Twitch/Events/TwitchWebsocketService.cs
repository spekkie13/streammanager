using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch.Auth;
using SpekkieTwitchBot.Twitch.Commands;
using SpekkieTwitchBot.Twitch.Events.Handlers;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using OnChannelPointsRewardRedeemedArgs = SpekkieTwitchBot.Models.Twitch.Pubsub.Args.OnChannelPointsRewardRedeemedArgs;

namespace SpekkieTwitchBot.Twitch.Events;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly Logger _GeneralLogger;
    private readonly CustomTwitchClient _TwitchClient;
    private readonly CustomPubsub _TwitchPubSub;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    private readonly GeneralCommandHandler _GeneralCommandHandler;
    private readonly TwitchUserAuth _twitchUserAuth;
    private readonly GeneralTwitchAuth _generalTwitchAuth;

    private readonly SubEventHandler _SubEventHandler;
    private readonly FollowEventHandler _FollowEventHandler;
    private readonly ChannelPointHandler _ChannelPointHandler;

    public TwitchWebsocketService(
        IConfiguration configuration, 
        ILogger<TwitchWebsocketService> logger,
        Logger generalLogger,
        TwitchAuthService twitchAuthService, 
        CustomTwitchClient twitchClient, 
        CustomPubsub twitchPubSub,
        SpotifyCommandHandler spotifyCommandHandler, 
        GeneralCommandHandler generalCommandHandler,
        SubEventHandler subEventHandler,
        FollowEventHandler followEventHandler,
        ChannelPointHandler channelPointHandler
        )
    {
        _Configuration = configuration;
        _GeneralLogger = generalLogger;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _SpotifyCommandHandler = spotifyCommandHandler;
        _GeneralCommandHandler = generalCommandHandler;
        
        _twitchUserAuth = twitchAuthService.GetTwitchUserAuth();
        _generalTwitchAuth = twitchAuthService.GetGeneralTwitchAuth();
        
        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        _TwitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));
        
        _SubEventHandler = subEventHandler;
        _FollowEventHandler = followEventHandler;
        _ChannelPointHandler = channelPointHandler;
        
        SetupTwitchClient();
        SetupPubSub();
    }

    private void SetupTwitchClient()
    {
        ConnectionCredentials cred = new ConnectionCredentials(TwitchConstants.ChannelName, _generalTwitchAuth.Implicit_OAuth);
        _TwitchClient.Initialize(cred, _generalTwitchAuth.BroadcasterName);
    }
    
    private void SetupPubSub()
    {
        _TwitchPubSub.OnPubSubServiceConnected += OnPubSubConnected;
        _TwitchPubSub.OnListenResponse += OnListenResponse;
        _TwitchPubSub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        _TwitchClient.OnChatCommandReceived += OnChatCommandReceived;
        _TwitchClient.OnFailureToReceiveJoinConfirmation += FailureToJoin;
        _TwitchClient.OnMessageReceived += OnMessageReceived;
        _TwitchPubSub.OnChannelSubscription += _SubEventHandler.HandleSub;
        _TwitchPubSub.OnFollow += _FollowEventHandler.HandleFollow;
        
        SetupTopics(); 
    }

    private void SetupTopics()
    {
        _TwitchPubSub.ListenToVideoPlayback(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToFollows(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToSubscriptions(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToLeaderboards(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToPredictions(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToRaid(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToChannelPoints(_generalTwitchAuth.ChannelId);
        _TwitchPubSub.ListenToBitsEventsV2(_generalTwitchAuth.ChannelId);
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        _GeneralLogger.LogInfo(e.ChatMessage.Message);
    }
    
    private void FailureToJoin(object? sender, OnFailureToReceiveJoinConfirmationArgs e)
    {
        _GeneralLogger.LogInfo($"Failed to join: {e.Exception.Channel} - {e.Exception.Details}");
    }
    
    private void OnPubSubConnected(object? sender, EventArgs e)
    {
        _TwitchPubSub.SendTopics(_twitchUserAuth.UserToken);
        _Logger.LogInformation("Pubsub Service Connected");
    }
    
    private void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        _GeneralLogger.LogInfo(e.Successful
            ? $"Successfully listening to: {e.Topic}"
            : $"Failed to listen to {e.Topic}! Error: {e.Response.Error}");
    }
    
    private void OnChannelPointsRewardRedeemed(object? sender, OnChannelPointsRewardRedeemedArgs e)
    {
        Redemption reward = e.RewardRedeemed.Redemption;
        switch (e.RewardRedeemed.Redemption.Reward.Title)
        {
            case "Song Request":
                bool success = _SpotifyCommandHandler.HandleAddSongToQueueCommand(reward.UserInput);
                HttpResponseMessage message = _ChannelPointHandler.HandleSongRedemption(
                         success: success, 
                    redemptionId: e.RewardRedeemed.Redemption.Id, 
                        rewardId: e.RewardRedeemed.Redemption.Reward.Id);
                _GeneralLogger.LogInfo(message.ToString());
                break;
        }
        _GeneralLogger.LogInfo($"Redeemed: {e.RewardRedeemed.Redemption.Reward.Title}");
    }
    
    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        _GeneralCommandHandler.HandleCommand(e.Command);
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