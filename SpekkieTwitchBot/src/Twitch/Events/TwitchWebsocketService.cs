using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch;
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
    private readonly TwitchAuth _TwitchAuth;

    public TwitchWebsocketService(
        IConfiguration configuration, 
        ILogger<TwitchWebsocketService> logger,
        Logger generalLogger,
        AuthService authService, 
        CustomTwitchClient twitchClient, 
        CustomPubsub twitchPubSub,
        SpotifyCommandHandler spotifyCommandHandler, 
        GeneralCommandHandler generalCommandHandler)
    {
        _Configuration = configuration;
        _GeneralLogger = generalLogger;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _SpotifyCommandHandler = spotifyCommandHandler;
        _GeneralCommandHandler = generalCommandHandler;

        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        _TwitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));

        _TwitchAuth = authService.SetupAuth();
        SetupTwitchClient();
        SetupPubSub();
    }

    private void SetupTwitchClient()
    {
        ConnectionCredentials cred = new ConnectionCredentials(TwitchConstants.ChannelName, _TwitchAuth.Implicit_OAuth);
        _TwitchClient.Initialize(cred, _TwitchAuth.BroadcasterName);
        _TwitchPubSub.OnChannelSubscription += SubEventHandler.HandleSub;
    }
    
    private void SetupPubSub()
    {
        _TwitchPubSub.OnPubSubServiceConnected += OnPubSubConnected;
        _TwitchPubSub.OnListenResponse += OnListenResponse;
        _TwitchPubSub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        _TwitchClient.OnChatCommandReceived += OnChatCommandReceived;
            
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
        _Logger.LogInformation("Pubsub Service Connected");
    }
    
    private void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        _GeneralLogger.LogInfo(e.Successful
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
                string status = success ? "FULFILLED" : "REJECTED";
                 UpdateRedemption(id: e.RewardRedeemed.Redemption.Id, 
                       broadcasterId: _TwitchAuth.ChannelId,
                            rewardId: e.RewardRedeemed.Redemption.Reward.Id, 
                              status: status);
                break;
        }
        _GeneralLogger.LogInfo($"Redeemed: {e.RewardRedeemed.Redemption.Reward.Title}");
    }
    
    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        _GeneralCommandHandler.HandleCommand(e.Command);
    }

    private async void UpdateRedemption(string id, string broadcasterId, string rewardId, string status)
    {
        using HttpClient client = new HttpClient(); 
        client.DefaultRequestHeaders.Add("Client-Id", _TwitchAuth.ClientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{_TwitchAuth.AppToken}");
            
        var requestContent = new StringContent($"{{\"status\":\"{status}\"}}", 
            Encoding.UTF8, 
            "application/json");
        
        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}?broadcaster_id={broadcasterId}&reward_id={rewardId}&id={id}";
        
        HttpResponseMessage message = await client.PatchAsync(requestUrl, requestContent);
            
        _GeneralLogger.LogInfo(message.ToString());
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