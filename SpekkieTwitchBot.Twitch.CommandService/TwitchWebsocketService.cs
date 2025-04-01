using CommandService.CommandHandlers;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieClassLibrary.Twitch.Pubsub.Args;
using SpekkieClassLibrary.Twitch.Pubsub.Events.Args;
using SpekkieClassLibrary.Twitch.Pubsub.Types;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchAuthService;
using TwitchAuthService.Events;
using TwitchAuthService.Events.Pubsub;
using TwitchAuthService.Handlers;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub.Events;
using OnEmoteOnlyArgs = TwitchLib.Client.Events.OnEmoteOnlyArgs;
using OnPubsubEmoteOnlyArgs = TwitchLib.PubSub.Events.OnEmoteOnlyArgs;
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;
using OnPubsubLogArgs = TwitchLib.PubSub.Events.OnLogArgs;

namespace CommandService;

public class TwitchWebsocketService : IHostedService
{
    private readonly Logger _GeneralLogger;
    private readonly CustomPubsub _CustomPubsub;
    private readonly CustomTwitchHttpClient _CustomTwitchHttpClient;
    private readonly GeneralTwitchAuth _generalTwitchAuth;
    private readonly TwitchUserAuth _twitchUserAuth;

    private readonly CustomTwitchClient _CustomTwitchClient;
    private readonly GeneralCommandHandler _GeneralCommandHandler;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    private readonly TextCommandHandler _TextCommandHandler;

    private readonly ChannelPointHandler _ChannelPointHandler;
    private readonly SubEventHandler _SubEventHandler;
    private readonly FollowEventHandler _FollowEventHandler;
    
    private readonly TwitchFileWriter _TwitchFileWriter;

    public TwitchWebsocketService(
        Logger generalLogger,
        TwitchAuthService.TwitchAuthService twitchAuthService,
        CustomTwitchClient customTwitchClient,
        CustomPubsub customPubsub,
        TextCommandHandler textCommandHandler,
        SpotifyCommandHandler spotifyCommandHandler,
        GeneralCommandHandler generalCommandHandler,
        SubEventHandler subEventHandler,
        FollowEventHandler followEventHandler,
        ChannelPointHandler channelPointHandler,
        CustomTwitchHttpClient customTwitchHttpClient,
        TwitchFileWriter twitchFileWriter
    )
    {
        _GeneralLogger = generalLogger;

        _SpotifyCommandHandler = spotifyCommandHandler;
        _GeneralCommandHandler = generalCommandHandler;
        _TextCommandHandler = textCommandHandler;

        _twitchUserAuth = twitchAuthService.GetTwitchUserAuth() ??
                          throw new ArgumentNullException(nameof(_twitchUserAuth));
        _generalTwitchAuth = twitchAuthService.GetGeneralTwitchAuth() ??
                             throw new ArgumentNullException(nameof(_generalTwitchAuth));

        _CustomTwitchClient = customTwitchClient ?? throw new ArgumentNullException(nameof(customTwitchClient));
        _CustomPubsub = customPubsub ?? throw new ArgumentNullException(nameof(customPubsub));
        _CustomTwitchHttpClient = customTwitchHttpClient ?? throw new ArgumentNullException(nameof(customTwitchHttpClient));
        
        _SubEventHandler = subEventHandler;
        _FollowEventHandler = followEventHandler;
        _ChannelPointHandler = channelPointHandler;
        
        _TwitchFileWriter = twitchFileWriter ?? throw new ArgumentNullException(nameof(twitchFileWriter));

        SetupTwitchClient();
        SetupPubSub();
        UpdateTwitchInfo();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _CustomTwitchClient.Connect();
            _CustomPubsub.Connect();
            await _CustomTwitchHttpClient.GetFollowerCount();
            await _CustomTwitchHttpClient.GetSubscriberCount();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _GeneralLogger.LogInfo("TwitchWebsocketService was canceled.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _GeneralLogger.LogInfo("Stopping WebSocket service...");

        try
        {
             _CustomPubsub.Disconnect(); // Ensure proper cleanup
        }
        catch (TaskCanceledException)
        {
            _GeneralLogger.LogInfo("WebSocket service stopped due to cancellation.");
        }
        catch (Exception ex)
        {
            _GeneralLogger.LogError($"Error while stopping WebSocket service. Message: {ex.Message}");
        }
        return Task.CompletedTask;
    }
    
    #region Setup
    private void UpdateTwitchInfo()
    {
        string recentFollower = _CustomTwitchHttpClient.GetLatestFollower().Result;
        _TwitchFileWriter.WriteMostRecentFollowerFile(recentFollower);
        
        int totalFollowers = _CustomTwitchHttpClient.GetFollowerCount().Result;
        _TwitchFileWriter.WriteTotalFollowersFile(totalFollowers);
        
        string recentSubscriber = _CustomTwitchHttpClient.GetLatestSubscriber().Result;
        _TwitchFileWriter.WriteMostRecentSubscriberFile(recentSubscriber);
        
        int totalSubscribers = _CustomTwitchHttpClient.GetSubscriberCount().Result;
        _TwitchFileWriter.WriteTotalSubscribersFile(totalSubscribers);
    }
    
    private void SetupTwitchClient()
    {
        ConnectionCredentials cred = new (twitchUsername: TwitchConstants.ChannelName, _generalTwitchAuth.ImplicitOAuth);
        _CustomTwitchClient.Initialize(cred, _generalTwitchAuth.BroadcasterName);
        _CustomTwitchClient.OnChatCommandReceived += OnChatCommandReceived;
        _CustomTwitchClient.OnFailureToReceiveJoinConfirmation += FailureToJoin;
        _CustomTwitchClient.OnMessageReceived += OnMessageReceived;
        _CustomTwitchClient.OnAnnouncement += OnAnnouncement;
        _CustomTwitchClient.OnVIPsReceived += OnVIPsReceived;
        _CustomTwitchClient.OnLog += OnTwitchClientLog;
        _CustomTwitchClient.OnConnected += OnConnected;
        _CustomTwitchClient.OnJoinedChannel += OnJoinedChannel;
        _CustomTwitchClient.OnIncorrectLogin += OnIncorrectLogin;
        _CustomTwitchClient.OnChannelStateChanged += OnChannelStateChanged;
        _CustomTwitchClient.OnUserStateChanged += OnUserStateChanged;
        _CustomTwitchClient.OnWhisperReceived += OnWhisperReceived;
        _CustomTwitchClient.OnMessageSent += OnMessageSent;
        _CustomTwitchClient.OnWhisperSent += OnWhisperSent;
        _CustomTwitchClient.OnWhisperCommandReceived += OnWhisperCommandReceived;
        _CustomTwitchClient.OnUserJoined += OnUserJoined;
        _CustomTwitchClient.OnUserLeft += OnUserLeft;
        _CustomTwitchClient.OnModeratorJoined += OnModeratorJoined;
        _CustomTwitchClient.OnModeratorLeft += OnModeratorLeft;
        _CustomTwitchClient.OnMessageCleared += OnMessageCleared;
        _CustomTwitchClient.OnNewSubscriber += OnNewSubscriber;
        _CustomTwitchClient.OnReSubscriber += OnReSubscriber;
        _CustomTwitchClient.OnPrimePaidSubscriber += OnPrimePaidSubscriber;
        _CustomTwitchClient.OnGiftedSubscription += OnGiftedSubscription;
        _CustomTwitchClient.OnCommunitySubscription += OnCommunitySubscription;
        _CustomTwitchClient.OnContinuedGiftedSubscription += OnContinuedGiftedSubscription;
        _CustomTwitchClient.OnExistingUsersDetected += OnExistingUsersDetected;
        _CustomTwitchClient.OnDisconnected += OnDisconnected;
        _CustomTwitchClient.OnConnectionError += OnConnectionError;
        _CustomTwitchClient.OnChatCleared += OnChatCleared;
        _CustomTwitchClient.OnUserTimedout += OnUserTimedout;
        _CustomTwitchClient.OnLeftChannel += OnLeftChannel;
        _CustomTwitchClient.OnUserBanned += OnUserBanned;
        _CustomTwitchClient.OnModeratorsReceived += OnModeratorsReceived;
        _CustomTwitchClient.OnChatColorChanged += OnChatColorChanged;
        _CustomTwitchClient.OnSendReceiveData += OnSendReceiveData;
        _CustomTwitchClient.OnRaidNotification += OnRaidNotification;
        _CustomTwitchClient.OnMessageThrottled += OnMessageThrottled;
        _CustomTwitchClient.OnWhisperThrottled += OnWhisperThrottled;
        _CustomTwitchClient.OnError += OnError;
        _CustomTwitchClient.OnReconnected += OnReconnected;
        _CustomTwitchClient.OnRequiresVerifiedEmail += OnRequiresVerifiedEmail;
        _CustomTwitchClient.OnRequiresVerifiedPhoneNumber += OnRequiresVerifiedPhoneNumber;
        _CustomTwitchClient.OnRateLimit += OnRateLimit;
        _CustomTwitchClient.OnDuplicate += OnDuplicate;
        _CustomTwitchClient.OnBannedEmailAlias += OnBannedEmailAlias;
        _CustomTwitchClient.OnSelfRaidError += OnSelfRaidError;
        _CustomTwitchClient.OnNoPermissionError += OnNoPermissionError;
        _CustomTwitchClient.OnRaidedChannelIsMatureAudience += OnRaidedChannelIsMatureAudience;
        _CustomTwitchClient.OnFollowersOnly += OnFollowerOnly;
        _CustomTwitchClient.OnSubsOnly += OnSubsOnly;
        _CustomTwitchClient.OnEmoteOnly += OnEmoteOnly;
        _CustomTwitchClient.OnSuspended += OnSuspended;
        _CustomTwitchClient.OnBanned += OnBanned;
        _CustomTwitchClient.OnSlowMode += OnSlowMode;
        _CustomTwitchClient.OnR9KMode += R9KMode;
        _CustomTwitchClient.OnUserIntro += OnUserIntro;
        _CustomTwitchClient.OnUnaccountedFor += OnUnaccountedFor;
    }

    private void SetupPubSub()
    {
        _CustomPubsub.OnPubSubServiceConnected += OnPubSubConnected;
        _CustomPubsub.OnListenResponse += OnListenResponse;
        _CustomPubsub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        _CustomPubsub.OnChannelSubscription += _SubEventHandler.HandleSub;
        _CustomPubsub.OnFollow += _FollowEventHandler.HandleFollow;
        _CustomPubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
        _CustomPubsub.OnPubSubServiceError += OnPubSubServiceError;
        _CustomPubsub.OnPubSubServiceClosed += OnPubSubServiceClosed;
        _CustomPubsub.OnTimeout += OnTimeout;
        _CustomPubsub.OnBan += OnBan;
        _CustomPubsub.OnMessageDeleted += OnMessageDeleted;
        _CustomPubsub.OnUnban += OnUnBan;
        _CustomPubsub.OnUntimeout += OnUnTimeout;
        _CustomPubsub.OnHost += OnHost;
        _CustomPubsub.OnSubscribersOnly += OnSubscribersOnly;
        _CustomPubsub.OnSubscribersOnlyOff += OnSubscribersOnlyOff;
        _CustomPubsub.OnClear += OnClear;
        _CustomPubsub.OnEmoteOnly += OnPubsubEmoteOnly;
        _CustomPubsub.OnEmoteOnlyOff += OnPubsubEmoteOnlyOff;
        _CustomPubsub.OnR9KBeta += R9KBeta;
        _CustomPubsub.OnR9KBetaOff += R9KBetaOff;
        _CustomPubsub.OnBitsReceived += OnBitsReceived;
        _CustomPubsub.OnBitsReceivedV2 += OnBitsReceivedV2;
        _CustomPubsub.OnStreamUp += OnStreamUp;
        _CustomPubsub.OnStreamDown += OnStreamDown;
        _CustomPubsub.OnViewCount += OnViewCount;
        _CustomPubsub.OnWhisper += OnWhisper;
        _CustomPubsub.OnChannelExtensionBroadcast += OnChannelExtensionBroadcast;
        _CustomPubsub.OnLeaderboardSubs += OnLeaderboardSubs;
        _CustomPubsub.OnLeaderboardBits += OnLeaderboardBits;
        _CustomPubsub.OnRaidUpdate += OnRaidUpdate;
        _CustomPubsub.OnRaidUpdateV2 += OnRaidUpdateV2;
        _CustomPubsub.OnRaidGo += OnRaidGo;
        _CustomPubsub.OnLog += OnPubsubLog;
        _CustomPubsub.OnCommercial += OnCommercial;
        _CustomPubsub.OnPrediction += OnPrediction;
        _CustomPubsub.OnAutomodCaughtMessage += OnAutomodCaughtMessage;
        _CustomPubsub.OnAutomodCaughtUserMessage += OnAutomodCaughtUserMessage;

        SetupTopics();
    }

    private void SetupTopics()
    {
        _CustomPubsub.ListenToVideoPlayback(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToFollows(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToSubscriptions(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToLeaderboards(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToPredictions(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToRaid(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToChannelPoints(_generalTwitchAuth.ChannelId);
        _CustomPubsub.ListenToBitsEventsV2(_generalTwitchAuth.ChannelId);
    }
    #endregion

    #region TwitchClientEvents

    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        string messageId = e.Command.ChatMessage.Id;
        string reply = _TextCommandHandler.HandleCommand(e.Command);

        if (e.Command.CommandText == "addcom")
        {
            string commandText = e.Command.ArgumentsAsString.Split("|")[0];
            string replyMessage = e.Command.ArgumentsAsString.Split("|")[1];
            _TextCommandHandler.AddCommand(commandText, replyMessage);
            reply = $"command {commandText} was added";
        }
        
        if (reply == "Unknown Command")
            reply = _GeneralCommandHandler.HandleCommand(e.Command);

        _CustomTwitchClient.SendReply(e.Command.ChatMessage.Channel, messageId, reply);
    }
    
    private void FailureToJoin(object? sender, OnFailureToReceiveJoinConfirmationArgs e)
    {
        _GeneralLogger.LogInfo($"Failed to join: {e.Exception.Channel} - {e.Exception.Details}");
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        _GeneralLogger.LogInfo(e.ChatMessage.Message);
    }

    private void OnAnnouncement(object? sender, OnAnnouncementArgs e)
    {
        _GeneralLogger.LogInfo($"New announcement: {e.Announcement}");
    }

    private void OnVIPsReceived(object? sender, OnVIPsReceivedArgs e)
    {
        _GeneralLogger.LogInfo($"VIPs received: {e.VIPs}");
    }

    private void OnTwitchClientLog(object? sender, OnLogArgs e)
    {
        _GeneralLogger.LogInfo($"New log: {e.Data}");
    }

    private void OnConnected(object? sender, OnConnectedArgs e)
    {
        _GeneralLogger.LogInfo($"TwitchClient connection established: {e.AutoJoinChannel}");
    }

    private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        _GeneralLogger.LogInfo($"Joined channel: {e.Channel} as {e.BotUsername}");
    }

    private void OnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
    {
        _GeneralLogger.LogInfo($"Incorrect login attempt: {e.Exception}");
    }

    private void OnChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
    {
        _GeneralLogger.LogInfo($"Channel State changed to {e.ChannelState.Channel}");
    }

    private void OnUserStateChanged(object? sender, OnUserStateChangedArgs e)
    {
        _GeneralLogger.LogInfo($"User state changed to {e.UserState}");
    }

    private void OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
    {
        _GeneralLogger.LogInfo($"New whisper received: {e.WhisperMessage}");
    }

    private void OnMessageSent(object? sender, OnMessageSentArgs e)
    {
        _GeneralLogger.LogInfo($"New message sent: {e.SentMessage.Message}");
    }

    private void OnWhisperSent(object? sender, OnWhisperSentArgs e)
    {
        _GeneralLogger.LogInfo($"New whisper message sent: {e.Message}");
    }

    private void OnWhisperCommandReceived(object? sender, OnWhisperCommandReceivedArgs e)
    {
        _GeneralLogger.LogInfo($"New whisper command received: {e.Command}");
    }

    private void OnUserJoined(object? sender, OnUserJoinedArgs e)
    {
        _GeneralLogger.LogInfo($"New user joined: {e.Username}");
    }

    private void OnUserLeft(object? sender, OnUserLeftArgs e)
    {
        _GeneralLogger.LogInfo($"User left: {e.Username}");
    }

    private void OnModeratorJoined(object? sender, OnModeratorJoinedArgs e)
    {
        _GeneralLogger.LogInfo($"Moderator joined: {e.Username}");
    }

    private void OnModeratorLeft(object? sender, OnModeratorLeftArgs e)
    {
        _GeneralLogger.LogInfo($"Moderator left: {e.Username}");
    }

    private void OnMessageCleared(object? sender, OnMessageClearedArgs e)
    {
        _GeneralLogger.LogInfo($"Message cleared: {e.Message}");
    }

    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        _GeneralLogger.LogInfo($"New subscriber: {e.Subscriber.DisplayName}");
    }

    private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
    {
        _GeneralLogger.LogInfo($"Re-subscription: {e.ReSubscriber.DisplayName} subscribed for {e.ReSubscriber.Months}");
    }

    private void OnPrimePaidSubscriber(object? sender, OnPrimePaidSubscriberArgs e)
    {
        _GeneralLogger.LogInfo($"New prime sub: {e.PrimePaidSubscriber.DisplayName}");
    }

    private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
    {
        _GeneralLogger.LogInfo(
            $"New gifted subscription: {e.GiftedSubscription.DisplayName} has just been gifted a subscription");
    }

    private void OnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs e)
    {
        _GeneralLogger.LogInfo(
            $"New community subscription: {e.GiftedSubscription.DisplayName} just got gifted a subscription");
    }

    private void OnContinuedGiftedSubscription(object? sender, OnContinuedGiftedSubscriptionArgs e)
    {
        _GeneralLogger.LogInfo(
            $"{e.ContinuedGiftedSubscription.DisplayName} continued their gifted subscription to your channel");
    }

    private void OnExistingUsersDetected(object? sender, OnExistingUsersDetectedArgs e)
    {
        string users = string.Join(",", e.Users.ToList());
        _GeneralLogger.LogInfo($"Existing user detected: {users}");
    }

    private void OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        _GeneralLogger.LogInfo("Disconnected");
    }

    private void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        _GeneralLogger.LogInfo($"Connection error: {e.Error.Message}");
    }

    private void OnChatCleared(object? sender, OnChatClearedArgs e)
    {
        _GeneralLogger.LogInfo($"Chat cleared in {e.Channel}");
    }

    private void OnUserTimedout(object? sender, OnUserTimedoutArgs e)
    {
        _GeneralLogger.LogInfo(
            $"{e.UserTimeout.Username} timed out for {e.UserTimeout.TimeoutReason} for the duration {e.UserTimeout.TimeoutDuration} seconds");
    }

    private void OnLeftChannel(object? sender, OnLeftChannelArgs e)
    {
        _GeneralLogger.LogInfo($"{e.BotUsername} left {e.Channel}");
    }

    private void OnUserBanned(object? sender, OnUserBannedArgs e)
    {
        _GeneralLogger.LogInfo($"{e.UserBan.Username} banned for {e.UserBan.BanReason}");
    }

    private void OnModeratorsReceived(object? sender, OnModeratorsReceivedArgs e)
    {
        _GeneralLogger.LogInfo($"Moderators received: {e.Moderators}");
    }

    private void OnChatColorChanged(object? sender, OnChatColorChangedArgs e)
    {
        _GeneralLogger.LogInfo($"Chat color changed: {e.Channel}");
    }

    private void OnSendReceiveData(object? sender, OnSendReceiveDataArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Data} - {e.Direction}");
    }

    private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        _GeneralLogger.LogInfo($"New raid: {e.RaidNotification.DisplayName} just raided!");
    }

    private void OnMessageThrottled(object? sender, OnMessageThrottledEventArgs e)
    {
        _GeneralLogger.LogInfo($"Message {e.Message} throttled");
    }

    private void OnWhisperThrottled(object? sender, OnWhisperThrottledEventArgs e)
    {
        _GeneralLogger.LogInfo($"Whisper throttled {e.Message}");
    }

    private void OnError(object? sender, OnErrorEventArgs e)
    {
        _GeneralLogger.LogInfo($"Error Occurred: {e.Exception.Message}");
    }

    private void OnReconnected(object? sender, OnReconnectedEventArgs e)
    {
        _GeneralLogger.LogInfo("Reconnected");
    }

    private void OnRequiresVerifiedEmail(object? sender, OnRequiresVerifiedEmailArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel} requires verified email");
    }

    private void OnRequiresVerifiedPhoneNumber(object? sender, OnRequiresVerifiedPhoneNumberArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel} requires verified phone number");
    }

    private void OnRateLimit(object? sender, OnRateLimitArgs e)
    {
        _GeneralLogger.LogInfo($"Rate limit occurred: {e.Channel}");
    }

    private void OnDuplicate(object? sender, OnDuplicateArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel} - duplicate occurred");
    }

    private void OnBannedEmailAlias(object? sender, OnBannedEmailAliasArgs e)
    {
        _GeneralLogger.LogInfo("Banned email alias detected");
    }

    private void OnSelfRaidError(object? sender, EventArgs e)
    {
        _GeneralLogger.LogInfo("Cannot raid your own channel");
    }

    private void OnNoPermissionError(object? sender, EventArgs e)
    {
        _GeneralLogger.LogInfo("No permission to perform this action");
    }

    private void OnRaidedChannelIsMatureAudience(object? sender, EventArgs e)
    {
        _GeneralLogger.LogInfo("Raided channel is mature audience");
    }

    private void OnFollowerOnly(object? sender, OnFollowersOnlyArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel}: triggered Follower-Only mode");
    }

    private void OnSubsOnly(object? sender, OnSubsOnlyArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel}: triggered Sub-Only mode");
    }

    private void OnEmoteOnly(object? sender, OnEmoteOnlyArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Channel}: triggered Emote-Only mode");
    }

    private void OnSuspended(object? sender, OnSuspendedArgs e)
    {
        _GeneralLogger.LogInfo($"New suspension event occurred in {e.Channel}");
    }

    private void OnBanned(object? sender, OnBannedArgs e)
    {
        _GeneralLogger.LogInfo($"New ban event occurred in {e.Channel}");
    }

    private void OnSlowMode(object? sender, OnSlowModeArgs e)
    {
        _GeneralLogger.LogInfo($"Slow mode triggered in {e.Channel}");
    }

    private void R9KMode(object? sender, OnR9kModeArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Message}");
    }

    private void OnUserIntro(object? sender, OnUserIntroArgs e)
    {
        _GeneralLogger.LogInfo($"New user introduction: {e.ChatMessage}");
    }

    private void OnUnaccountedFor(object? sender, OnUnaccountedForArgs e)
    {
        _GeneralLogger.LogInfo($"Unaccounted for event occurred: {e.Channel} - {e.Location}");
    }

    #endregion

    #region PubsubEvents

    private void OnPubSubConnected(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_twitchUserAuth.UserToken))
        {
            _GeneralLogger.LogInfo("User token is empty");
            return;
        }

        _CustomPubsub.SendTopics(_twitchUserAuth.UserToken);
        _GeneralLogger.LogInfo("Pubsub Service Connected");
    }

    private void OnListenResponse(object? sender, ListenResponseArgs e)
    {
        _GeneralLogger.LogInfo(e.Successful
            ? $"Successfully listening to: {e.Topic}"
            : $"Failed to listen to {e.Topic}! Error: {e.Response.Error}");
    }

    private void OnChannelPointsRewardRedeemed(object? sender, ChannelPointsRewardRedeemedArgs e)
    {
        Redemption? reward = e.RewardRedeemed?.Redemption;
        if (reward?.Reward == null ||
            string.IsNullOrEmpty(reward.UserInput) ||
            string.IsNullOrEmpty(reward.Reward.Id) ||
            string.IsNullOrEmpty(reward.Id)) return;
        switch (reward.Reward.Title)
        {
            case "Song Request":
                bool success = _SpotifyCommandHandler.HandleAddSongToQueueCommand(reward.UserInput).Contains("Added");
                HttpResponseMessage message = _ChannelPointHandler.HandleSongRedemption(
                    success,
                    reward.Id,
                    reward.Reward.Id);
                _GeneralLogger.LogInfo(message.ToString());
                break;
        }

        _GeneralLogger.LogInfo($"Redeemed: {reward.Reward.Title}");
    }

    private void OnPubSubServiceConnected(object? sender, EventArgs e)
    {
        _GeneralLogger.LogInfo("Pubsub connected");
    }

    private void OnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
    {
        _GeneralLogger.LogError($"an error occurred {e.Exception}");
    }

    private void OnPubSubServiceClosed(object? sender, EventArgs e)
    {
        _GeneralLogger.LogInfo("Pubsub service closed");
        _CustomPubsub.Connect();
    }

    private void OnTimeout(object? sender, OnTimeoutArgs e)
    {
        _GeneralLogger.LogInfo(
            $"User {e.TimedoutUser} has been timed out for {e.TimeoutDuration} for {e.TimeoutReason}");
    }

    private void OnBan(object? sender, OnBanArgs e)
    {
        _GeneralLogger.LogInfo($"User {e.BannedUser} has been banned for {e.BanReason}");
    }

    private void OnMessageDeleted(object? sender, OnMessageDeletedArgs e)
    {
        _GeneralLogger.LogInfo($"Message {e.Message} deleted by {e.DeletedBy}");
    }

    private void OnUnTimeout(object? sender, OnUntimeoutArgs e)
    {
        _GeneralLogger.LogInfo($"User {e.UntimeoutedUser} has been untimed out by {e.UntimeoutedBy} in {e.ChannelId}");
    }

    private void OnUnBan(object? sender, OnUnbanArgs e)
    {
        _GeneralLogger.LogInfo($"User {e.UnbannedUser} has been unbanned by {e.UnbannedBy}");
    }

    private void OnHost(object? sender, OnHostArgs e)
    {
        _GeneralLogger.LogInfo($"Host: {e.ChannelId} - {e.HostedChannel}");
    }

    private void OnSubscribersOnly(object? sender, OnSubscribersOnlyArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Moderator} turned on sub only");
    }

    private void OnSubscribersOnlyOff(object? sender, OnSubscribersOnlyOffArgs e)
    {
        _GeneralLogger.LogInfo($"{e.Moderator} turned off sub only");
    }

    private void OnClear(object? sender, OnClearArgs e)
    {
        _GeneralLogger.LogInfo($"Chat cleared by {e.Moderator}");
    }

    private void OnPubsubEmoteOnly(object? sender, OnPubsubEmoteOnlyArgs e)
    {
        _GeneralLogger.LogInfo($"Emote only turned on by {e.Moderator}");
    }

    private void OnPubsubEmoteOnlyOff(object? sender, OnEmoteOnlyOffArgs e)
    {
        _GeneralLogger.LogInfo($"Emote only turned off by {e.Moderator}");
    }

    private void R9KBeta(object? sender, OnR9kBetaArgs e)
    {
        _GeneralLogger.LogInfo($"R9kBeta - {e.ChannelId}");
    }

    private void R9KBetaOff(object? sender, OnR9kBetaOffArgs e)
    {
        _GeneralLogger.LogInfo($"R9kBeta off - {e.ChannelId}");
    }

    private void OnBitsReceived(object? sender, OnBitsReceivedArgs e)
    {
        _GeneralLogger.LogInfo(
            $"Bits received: {e.BitsUsed} cheered by {e.Username}, they have cheered {e.TotalBitsUsed} in {e.ChannelName}");
    }

    private void OnBitsReceivedV2(object? sender, BitsReceivedV2Args e)
    {
        _GeneralLogger.LogInfo(
            $"Bits received: {e.BitsUsed} cheered by {e.UserName}, they have cheered {e.TotalBitsUsed} in {e.ChannelName}");
    }

    private void OnStreamUp(object? sender, OnStreamUpArgs e)
    {
        _GeneralLogger.LogInfo($"Stream is up at: {e.ServerTime}");
    }

    private void OnStreamDown(object? sender, OnStreamDownArgs e)
    {
        _GeneralLogger.LogInfo($"Stream has gone down at: {e.ServerTime}");
    }

    private void OnViewCount(object? sender, OnViewCountArgs e)
    {
        _GeneralLogger.LogInfo($"View count: {e.Viewers}");
    }

    private void OnWhisper(object? sender, WhisperArgs e)
    {
        _GeneralLogger.LogInfo($"Whisper received: {e.Whisper}");
    }

    private void OnChannelExtensionBroadcast(object? sender, OnChannelExtensionBroadcastArgs e)
    {
        _GeneralLogger.LogInfo($"Channel extension broadcast: {string.Join(',', e.Messages)}");
    }

    private void OnLeaderboardSubs(object? sender, OnLeaderboardEventArgs e)
    {
        _GeneralLogger.LogInfo($"{e.TopList}");
    }

    private void OnLeaderboardBits(object? sender, OnLeaderboardEventArgs e)
    {
        _GeneralLogger.LogInfo($"{e.TopList}");
    }

    private void OnPubsubLog(object? sender, OnPubsubLogArgs e)
    {
        _GeneralLogger.LogInfo(e.Data);
    }

    private void OnRaidUpdate(object? sender, OnRaidUpdateArgs e)
    {
        _GeneralLogger.LogInfo($"Raid update: {e.RemainingDurationSeconds} until raid starts to {e.TargetChannelId}");
    }

    private void OnRaidUpdateV2(object? sender, OnRaidUpdateV2Args e)
    {
        _GeneralLogger.LogInfo($"Raid update: {e.TargetChannelId}");
    }

    private void OnRaidGo(object? sender, OnRaidGoArgs e)
    {
        _GeneralLogger.LogInfo($"Raid go: {e.TargetLogin} being raided now");
    }

    private void OnCommercial(object? sender, OnCommercialArgs e)
    {
        _GeneralLogger.LogInfo($"Commercial started at: {e.ServerTime} for {e.Length} seconds");
    }

    private void OnPrediction(object? sender, PredictionArgs e)
    {
        _GeneralLogger.LogInfo($"Prediction started: {e.Title} created at {e.CreatedAt}");
    }

    private void OnAutomodCaughtMessage(object? sender, AutomodCaughtMessageArgs e)
    {
        _GeneralLogger.LogInfo($"Message caught by automod {e.AutomodCaughtMessage}");
    }

    private void OnAutomodCaughtUserMessage(object? sender, AutomodCaughtUserMessage e)
    {
        _GeneralLogger.LogInfo($"User message caught by automod: {e.AutomodCaughtMessage}");
    }

    #endregion
}