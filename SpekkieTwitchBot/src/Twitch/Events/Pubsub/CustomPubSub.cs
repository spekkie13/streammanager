using System.Timers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Args;
using SpekkieClassLibrary.Twitch.Pubsub.Events.Args;
using SpekkieClassLibrary.Twitch.Pubsub.Types;
using SpekkieTwitchBot.Interfaces;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models;
using AutomodCaughtMessage = SpekkieClassLibrary.Twitch.Pubsub.Types.AutomodCaughtMessage;
using AutomodQueue = SpekkieClassLibrary.Twitch.Pubsub.Types.AutomodQueue;
using AutomodQueueType = SpekkieClassLibrary.Twitch.Pubsub.Enums.AutomodQueueType;
using ChannelBitsEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelBitsEvents;
using ChannelBitsEventsV2 = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelBitsEventsV2;
using ChannelExtensionBroadcast = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelExtensionBroadcast;
using ChannelPointsChannel = SpekkieClassLibrary.Twitch.Pubsub.Types.ChannelPointsChannel;
using ChannelPointsChannelType = SpekkieClassLibrary.Twitch.Pubsub.Enums.ChannelPointsChannelType;
using ChannelSubscription = SpekkieClassLibrary.Twitch.Pubsub.Types.ChannelSubscription;
using ChatModeratorActions = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChatModeratorActions;
using CommunityPointsChannel = SpekkieClassLibrary.Twitch.Pubsub.Types.CommunityPointsChannel;
using CommunityPointsChannelType = SpekkieClassLibrary.Twitch.Pubsub.Enums.CommunityPointsChannelType;
using Following = SpekkieClassLibrary.Twitch.Pubsub.Types.Following;
using LeaderboardEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.LeaderboardEvents;
using LeaderBoardType = SpekkieClassLibrary.Twitch.Pubsub.Enums.LeaderBoardType;
using RewardRedeemed = SpekkieClassLibrary.Twitch.Pubsub.Types.RewardRedeemed;
using PredictionEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.PredictionEvents;
using PredictionType = SpekkieClassLibrary.Twitch.Pubsub.Enums.PredictionType;
using RaidEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.RaidEvents;
using RaidType = SpekkieClassLibrary.Twitch.Pubsub.Enums.RaidType;
using UserModerationNotifications = SpekkieClassLibrary.Twitch.Pubsub.EventData.UserModerationNotifications;
using VideoPlayback = SpekkieClassLibrary.Twitch.Pubsub.Types.VideoPlayback;
using VideoPlaybackType = SpekkieClassLibrary.Twitch.Pubsub.Enums.VideoPlaybackType;
using Whisper = SpekkieClassLibrary.Twitch.Pubsub.EventData.Whisper;

#nullable disable
namespace SpekkieTwitchBot.Twitch.Events.Pubsub;

public class CustomPubsub : ITwitchPubSub
{
    private readonly WebSocketClient _socket;
    private readonly List<PreviousRequest> _previousRequests = new();
    private readonly Semaphore _previousRequestsSemaphore = new(1, 1);
    private readonly ILogger<CustomPubsub> _logger;
    private readonly System.Timers.Timer _pingTimer = new();
    private readonly System.Timers.Timer _pongTimer = new();
    private bool _pongReceived;
    private readonly List<string> _topicList = new();
    private readonly Dictionary<string, string> _topicToChannelId = new();
    private static readonly Random Random = new();
    
    public event EventHandler OnPubSubServiceConnected;
    public event EventHandler<OnPubSubServiceErrorArgs> OnPubSubServiceError;
    public event EventHandler OnPubSubServiceClosed;
    public event EventHandler<ListenResponseArgs> OnListenResponse;
    public event EventHandler<OnTimeoutArgs> OnTimeout;
    public event EventHandler<OnBanArgs> OnBan;
    public event EventHandler<OnMessageDeletedArgs> OnMessageDeleted;
    public event EventHandler<OnUnbanArgs> OnUnban;
    public event EventHandler<OnUntimeoutArgs> OnUntimeout;
    public event EventHandler<OnHostArgs> OnHost;
    public event EventHandler<OnSubscribersOnlyArgs> OnSubscribersOnly;
    public event EventHandler<OnSubscribersOnlyOffArgs> OnSubscribersOnlyOff;
    public event EventHandler<OnClearArgs> OnClear;
    public event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly;
    public event EventHandler<OnEmoteOnlyOffArgs> OnEmoteOnlyOff;
    public event EventHandler<OnR9kBetaArgs> R9KBeta;
    public event EventHandler<OnR9kBetaOffArgs> R9KBetaOff;
    public event EventHandler<OnBitsReceivedArgs> OnBitsReceived;
    public event EventHandler<BitsReceivedV2Args> OnBitsReceivedV2;
    public event EventHandler<OnStreamUpArgs> OnStreamUp;
    public event EventHandler<OnStreamDownArgs> OnStreamDown;
    public event EventHandler<OnViewCountArgs> OnViewCount;
    public event EventHandler<WhisperArgs> OnWhisper;
    public event EventHandler<ChannelSubscriptionArgs> OnChannelSubscription;
    public event EventHandler<OnChannelExtensionBroadcastArgs> OnChannelExtensionBroadcast;
    public event EventHandler<OnFollowArgs> OnFollow;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    public event EventHandler<OnCustomRewardCreatedArgs> OnCustomRewardCreated;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    public event EventHandler<OnCustomRewardUpdatedArgs> OnCustomRewardUpdated;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    public event EventHandler<OnCustomRewardDeletedArgs> OnCustomRewardDeleted;
    [Obsolete(
        "This event fires on an undocumented/retired/obsolete topic. Consider using OnChannelPointsRewardRedeemed",
        false)]
    public event EventHandler<OnRewardRedeemedArgs> OnRewardRedeemed;
    public event EventHandler<ChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
    public event EventHandler<OnLeaderboardEventArgs> OnLeaderboardSubs;
    public event EventHandler<OnLeaderboardEventArgs> OnLeaderboardBits;
    public event EventHandler<OnRaidUpdateArgs> OnRaidUpdate;
    public event EventHandler<OnRaidUpdateV2Args> OnRaidUpdateV2;
    public event EventHandler<OnRaidGoArgs> OnRaidGo;
    public event EventHandler<OnLogArgs> OnLog;
    public event EventHandler<OnCommercialArgs> OnCommercial;
    public event EventHandler<PredictionArgs> OnPrediction;
    public event EventHandler<AutomodCaughtMessageArgs> OnAutomodCaughtMessage;
    public event EventHandler<AutomodCaughtUserMessage> OnAutomodCaughtUserMessage;

    public CustomPubsub(ILogger<CustomPubsub> logger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _socket = new WebSocketClient(new ClientOptions
        {
            ClientType = ClientType.PubSub
        });
        _socket.OnConnected += Socket_OnConnected;
        _socket.OnError += OnError;
        _socket.OnMessage += OnMessage;
        _socket.OnDisconnected += Socket_OnDisconnected;
        _pongTimer.Interval = 15000.0;
        _pongTimer.Elapsed += PongTimerTick;
    }

    private void OnError(object sender, OnErrorEventArgs e)
    {
        ILogger<CustomPubsub> logger = _logger;
        if (logger != null)
            logger.LogError($"OnError in PubSub Websocket connection occured! Exception: {e.Exception}");
        EventHandler<OnPubSubServiceErrorArgs> pubSubServiceError = OnPubSubServiceError;
        if (pubSubServiceError == null)
            return;
        pubSubServiceError(this, new OnPubSubServiceErrorArgs
        {
            Exception = e.Exception
        });
    }

    private void OnMessage(object sender, OnMessageEventArgs e)
    {
        ILogger<CustomPubsub> logger = _logger;
        if (logger != null)
            logger.LogDebug("Received Websocket OnMessage: " + e.Message);
        EventHandler<OnLogArgs> onLog = OnLog;
        if (onLog != null)
            onLog(this, new OnLogArgs
            {
                Data = e.Message
            });
        ParseMessage(e.Message);
    }

    private void Socket_OnDisconnected(object sender, EventArgs e)
    {
        ILogger<CustomPubsub> logger = _logger;
        if (logger != null)
            logger.LogWarning("PubSub Websocket connection closed");
        _pingTimer.Stop();
        _pongTimer.Stop();
        EventHandler subServiceClosed = OnPubSubServiceClosed;
        if (subServiceClosed == null)
            return;
        subServiceClosed(this, EventArgs.Empty);
    }

    private void Socket_OnConnected(object sender, EventArgs e)
    {
        ILogger<CustomPubsub> logger = _logger;
        if (logger != null)
            logger.LogInformation("PubSub Websocket connection established");
        _pingTimer.Interval = 180000.0;
        _pingTimer.Elapsed += PingTimerTick;
        _pingTimer.Start();
        EventHandler serviceConnected = OnPubSubServiceConnected;
        if (serviceConnected == null)
            return;
        serviceConnected(this, EventArgs.Empty);
    }

    private void PingTimerTick(object sender, ElapsedEventArgs e)
    {
        _pongReceived = false;
        _socket.Send(new JObject(new JProperty("type", "PING")).ToString());
        _pongTimer.Start();
    }

    private void PongTimerTick(object sender, ElapsedEventArgs e)
    {
        _pongTimer.Stop();
        if (_pongReceived)
            _pongReceived = false;
        else
            _socket.Close();
    }

    private void ParseMessage(string message)
    {
        string type = JObject.Parse(message).SelectToken("type")?.ToString().ToLower() ?? "";
        if (string.IsNullOrEmpty(type)) return;
        switch (type)
        {
            case "response":
                Response response = new Response(message);
                if (_previousRequests.Count != 0)
                {
                    bool flag = false;
                    _previousRequestsSemaphore.WaitOne();
                    try
                    {
                        int index = 0;
                        while (index < _previousRequests.Count)
                        {
                            PreviousRequest previousRequest = _previousRequests[index];
                            if (string.Equals(previousRequest.Nonce, response.Nonce, StringComparison.CurrentCulture))
                            {
                                _previousRequests.RemoveAt(index);
                                _topicToChannelId.TryGetValue(previousRequest.Topic, out string str);
                                EventHandler<ListenResponseArgs> onListenResponse = OnListenResponse;
                                if (onListenResponse != null)
                                    onListenResponse(this, new ListenResponseArgs
                                    {
                                        Response = response,
                                        Topic = previousRequest.Topic,
                                        Successful = response.Successful,
                                        ChannelId = str
                                    });
                                flag = true;
                            }
                            else
                                ++index;
                        }
                    }
                    finally
                    {
                        _previousRequestsSemaphore.Release();
                    }

                    if (flag)
                        return;
                }

                break;
            case nameof(message):
                Message message1 = new Message(message);
                _topicToChannelId.TryGetValue(message1.Topic, out string str1);
                string str2 = str1 ?? "";
                switch (message1.Topic.Split('.')[0])
                {
                    case "automod-queue":
                        AutomodQueue messageData1 = (AutomodQueue) message1.MessageData;
                        switch (messageData1.Type)
                        {
                            case AutomodQueueType.CaughtMessage:
                                AutomodCaughtMessage
                                    data1 =
                                        messageData1.Data as AutomodCaughtMessage;
                                EventHandler<AutomodCaughtMessageArgs> automodCaughtMessage =
                                    OnAutomodCaughtMessage;
                                if (automodCaughtMessage == null)
                                    return;
                                automodCaughtMessage(this, new AutomodCaughtMessageArgs
                                {
                                    ChannelId = str2,
                                    AutomodCaughtMessage = data1
                                });
                                return;
                            case AutomodQueueType.Unknown:
                                UnaccountedFor("Unknown automod queue type. Msg: " + messageData1.RawData);
                                return;
                            default:
                                return;
                        }
                    case "channel-bits-events-v1":
                        if (message1.MessageData is ChannelBitsEvents messageData2)
                        {
                            EventHandler<OnBitsReceivedArgs> onBitsReceived = OnBitsReceived;
                            if (onBitsReceived == null)
                                return;
                            onBitsReceived(this, new OnBitsReceivedArgs
                            {
                                BitsUsed = messageData2.BitsUsed,
                                ChannelId = messageData2.ChannelId,
                                ChannelName = messageData2.ChannelName,
                                ChatMessage = messageData2.ChatMessage,
                                Context = messageData2.Context,
                                Time = messageData2.Time,
                                TotalBitsUsed = messageData2.TotalBitsUsed,
                                UserId = messageData2.UserId,
                                Username = messageData2.Username
                            });
                            return;
                        }

                        break;
                    case "channel-bits-events-v2":
                        if (message1.MessageData is ChannelBitsEventsV2 messageData3)
                        {
                            EventHandler<BitsReceivedV2Args> onBitsReceivedV2 = OnBitsReceivedV2;
                            if (onBitsReceivedV2 == null)
                                return;
                            onBitsReceivedV2(this, new BitsReceivedV2Args
                            {
                                IsAnonymous = messageData3.IsAnonymous,
                                BitsUsed = messageData3.BitsUsed,
                                ChannelId = messageData3.ChannelId,
                                ChannelName = messageData3.ChannelName,
                                ChatMessage = messageData3.ChatMessage,
                                Context = messageData3.Context,
                                Time = messageData3.Time,
                                TotalBitsUsed = messageData3.TotalBitsUsed,
                                UserId = messageData3.UserId,
                                UserName = messageData3.UserName
                            });
                            return;
                        }

                        break;
                    case "channel-ext-v1":
                        ChannelExtensionBroadcast messageData4 = (ChannelExtensionBroadcast) message1.MessageData;
                        EventHandler<OnChannelExtensionBroadcastArgs> extensionBroadcast =
                            OnChannelExtensionBroadcast;
                        if (extensionBroadcast == null)
                            return;
                        extensionBroadcast(this, new OnChannelExtensionBroadcastArgs
                        {
                            Messages = messageData4.Messages,
                            ChannelId = str2
                        });
                        return;
                    case "channel-points-channel-v1":
                        ChannelPointsChannel messageData5 = (ChannelPointsChannel) message1.MessageData;
                        switch (messageData5.Type)
                        {
                            case ChannelPointsChannelType.RewardRedeemed:
                                RewardRedeemed data2 = (RewardRedeemed) messageData5.Data;
                                EventHandler<ChannelPointsRewardRedeemedArgs> pointsRewardRedeemed =
                                    OnChannelPointsRewardRedeemed;
                                if (pointsRewardRedeemed == null)
                                    return;
                                pointsRewardRedeemed(this, new ChannelPointsRewardRedeemedArgs
                                {
                                    ChannelId = data2.Redemption.ChannelId,
                                    RewardRedeemed = data2
                                });
                                return;
                            case ChannelPointsChannelType.Unknown:
                                UnaccountedFor("Unknown channel points type. Msg: " + messageData5.RawData);
                                return;
                            default:
                                return;
                        }
                    case "channel-subscribe-events-v1":
                        ChannelSubscription messageData6 = message1.MessageData as ChannelSubscription;
                        EventHandler<ChannelSubscriptionArgs> channelSubscription = OnChannelSubscription;
                        if (channelSubscription == null)
                            return;
                        channelSubscription(this, new ChannelSubscriptionArgs
                        {
                            Subscription = messageData6,
                            ChannelId = str2
                        });
                        return;
                    case "chat_moderator_actions":
                        ChatModeratorActions messageData7 = message1.MessageData as ChatModeratorActions;
                        string str3 = "";
                        switch (messageData7?.ModerationAction.ToLower())
                        {
                            case "ban":
                                if (messageData7.Args.Count > 1)
                                    str3 = messageData7.Args[1];
                                EventHandler<OnBanArgs> onBan = OnBan;
                                if (onBan == null)
                                    return;
                                onBan(this, new OnBanArgs
                                {
                                    BannedBy = messageData7.CreatedBy,
                                    BannedByUserId = messageData7.CreatedByUserId,
                                    BannedUserId = messageData7.TargetUserId,
                                    BanReason = str3,
                                    BannedUser = messageData7.Args[0],
                                    ChannelId = str2
                                });
                                return;
                            case "clear":
                                EventHandler<OnClearArgs> onClear = OnClear;
                                if (onClear == null)
                                    return;
                                onClear(this, new OnClearArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "delete":
                                EventHandler<OnMessageDeletedArgs> onMessageDeleted = OnMessageDeleted;
                                if (onMessageDeleted == null)
                                    return;
                                onMessageDeleted(this, new OnMessageDeletedArgs
                                {
                                    DeletedBy = messageData7.CreatedBy,
                                    DeletedByUserId = messageData7.CreatedByUserId,
                                    TargetUserId = messageData7.TargetUserId,
                                    TargetUser = messageData7.Args[0],
                                    Message = messageData7.Args[1],
                                    MessageId = messageData7.Args[2],
                                    ChannelId = str2
                                });
                                return;
                            case "emoteonly":
                                EventHandler<OnEmoteOnlyArgs> onEmoteOnly = OnEmoteOnly;
                                if (onEmoteOnly == null)
                                    return;
                                onEmoteOnly(this, new OnEmoteOnlyArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "emoteonlyoff":
                                EventHandler<OnEmoteOnlyOffArgs> onEmoteOnlyOff = OnEmoteOnlyOff;
                                if (onEmoteOnlyOff == null)
                                    return;
                                onEmoteOnlyOff(this, new OnEmoteOnlyOffArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "host":
                                EventHandler<OnHostArgs> onHost = OnHost;
                                if (onHost == null)
                                    return;
                                onHost(this, new OnHostArgs
                                {
                                    HostedChannel = messageData7.Args[0],
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "r9kbeta":
                                EventHandler<OnR9kBetaArgs> onR9KBeta = R9KBeta;
                                if (onR9KBeta == null)
                                    return;
                                onR9KBeta(this, new OnR9kBetaArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "r9kbetaoff":
                                EventHandler<OnR9kBetaOffArgs> onR9KBetaOff = R9KBetaOff;
                                if (onR9KBetaOff == null)
                                    return;
                                onR9KBetaOff(this, new OnR9kBetaOffArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "subscribers":
                                EventHandler<OnSubscribersOnlyArgs> onSubscribersOnly = OnSubscribersOnly;
                                if (onSubscribersOnly == null)
                                    return;
                                onSubscribersOnly(this, new OnSubscribersOnlyArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "subscribersoff":
                                EventHandler<OnSubscribersOnlyOffArgs> subscribersOnlyOff = OnSubscribersOnlyOff;
                                if (subscribersOnlyOff == null)
                                    return;
                                subscribersOnlyOff(this, new OnSubscribersOnlyOffArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "timeout":
                                if (messageData7.Args.Count > 2)
                                    str3 = messageData7.Args[2];
                                EventHandler<OnTimeoutArgs> onTimeout = OnTimeout;
                                if (onTimeout == null)
                                    return;
                                onTimeout(this, new OnTimeoutArgs
                                {
                                    TimedoutBy = messageData7.CreatedBy,
                                    TimedoutById = messageData7.CreatedByUserId,
                                    TimedoutUserId = messageData7.TargetUserId,
                                    TimeoutDuration = TimeSpan.FromSeconds(int.Parse(messageData7.Args[1])),
                                    TimeoutReason = str3,
                                    TimedoutUser = messageData7.Args[0],
                                    ChannelId = str2
                                });
                                return;
                            case "unban":
                                EventHandler<OnUnbanArgs> onUnban = OnUnban;
                                if (onUnban == null)
                                    return;
                                onUnban(this, new OnUnbanArgs
                                {
                                    UnbannedBy = messageData7.CreatedBy,
                                    UnbannedByUserId = messageData7.CreatedByUserId,
                                    UnbannedUserId = messageData7.TargetUserId,
                                    UnbannedUser = messageData7.Args[0],
                                    ChannelId = str2
                                });
                                return;
                            case "untimeout":
                                EventHandler<OnUntimeoutArgs> onUntimeout = OnUntimeout;
                                if (onUntimeout == null)
                                    return;
                                onUntimeout(this, new OnUntimeoutArgs
                                {
                                    UntimeoutedBy = messageData7.CreatedBy,
                                    UntimeoutedByUserId = messageData7.CreatedByUserId,
                                    UntimeoutedUserId = messageData7.TargetUserId,
                                    UntimeoutedUser = messageData7.Args[0],
                                    ChannelId = str2
                                });
                                return;
                        }

                        break;
                    case "community-points-channel-v1":
                        CommunityPointsChannel messageData8 = (CommunityPointsChannel)message1.MessageData;
                        CommunityPointsChannelType? nullable1 = messageData8.Type;
                        switch (nullable1.GetValueOrDefault())
                        {
                            case CommunityPointsChannelType.RewardRedeemed:
                                EventHandler<OnRewardRedeemedArgs> onRewardRedeemed = OnRewardRedeemed;
                                if (onRewardRedeemed == null)
                                    return;
                                onRewardRedeemed(this, new OnRewardRedeemedArgs
                                {
                                    TimeStamp = messageData8.TimeStamp,
                                    ChannelId = messageData8.ChannelId,
                                    Login = messageData8.Login,
                                    DisplayName = messageData8.DisplayName,
                                    Message = messageData8.Message,
                                    RewardId = messageData8.RewardId,
                                    RewardTitle = messageData8.RewardTitle,
                                    RewardPrompt = messageData8.RewardPrompt,
                                    RewardCost = messageData8.RewardCost,
                                    Status = messageData8.Status,
                                    RedemptionId = messageData8.RedemptionId
                                });
                                return;
                            case CommunityPointsChannelType.CustomRewardUpdated:
                                EventHandler<OnCustomRewardUpdatedArgs>
                                    customRewardUpdated = OnCustomRewardUpdated;
                                if (customRewardUpdated == null)
                                    return;
                                customRewardUpdated(this, new OnCustomRewardUpdatedArgs
                                {
                                    TimeStamp = messageData8.TimeStamp,
                                    ChannelId = messageData8.ChannelId,
                                    RewardId = messageData8.RewardId,
                                    RewardTitle = messageData8.RewardTitle,
                                    RewardPrompt = messageData8.RewardPrompt,
                                    RewardCost = messageData8.RewardCost
                                });
                                return;
                            case CommunityPointsChannelType.CustomRewardCreated:
                                EventHandler<OnCustomRewardCreatedArgs>
                                    customRewardCreated = OnCustomRewardCreated;
                                if (customRewardCreated == null)
                                    return;
                                customRewardCreated(this, new OnCustomRewardCreatedArgs
                                {
                                    TimeStamp = messageData8.TimeStamp,
                                    ChannelId = messageData8.ChannelId,
                                    RewardId = messageData8.RewardId,
                                    RewardTitle = messageData8.RewardTitle,
                                    RewardPrompt = messageData8.RewardPrompt,
                                    RewardCost = messageData8.RewardCost
                                });
                                return;
                            case CommunityPointsChannelType.CustomRewardDeleted:
                                EventHandler<OnCustomRewardDeletedArgs>
                                    customRewardDeleted = OnCustomRewardDeleted;
                                if (customRewardDeleted == null)
                                    return;
                                customRewardDeleted(this, new OnCustomRewardDeletedArgs
                                {
                                    TimeStamp = messageData8.TimeStamp,
                                    ChannelId = messageData8.ChannelId,
                                    RewardId = messageData8.RewardId,
                                    RewardTitle = messageData8.RewardTitle,
                                    RewardPrompt = messageData8.RewardPrompt
                                });
                                return;
                            default:
                                return;
                        }
                    case "following":
                        Following messageData9 = (Following) message1.MessageData;
                        messageData9.FollowedChannelId = message1.Topic.Split('.')[1];
                        EventHandler<OnFollowArgs> onFollow = OnFollow;
                        if (onFollow == null)
                            return;
                        onFollow(this, new OnFollowArgs
                        {
                            FollowedChannelId = messageData9.FollowedChannelId,
                            DisplayName = messageData9.DisplayName,
                            UserId = messageData9.UserId,
                            Username = messageData9.Username
                        });
                        return;
                    case "leaderboard-events-v1":
                        LeaderboardEvents messageData10 = (LeaderboardEvents)message1.MessageData;
                        LeaderBoardType? nullable2 = messageData10.Type;
                        switch (nullable2.GetValueOrDefault())
                        {
                            case LeaderBoardType.BitsUsageByChannel:
                                EventHandler<OnLeaderboardEventArgs> onLeaderboardBits = OnLeaderboardBits;
                                if (onLeaderboardBits == null)
                                    return;
                                onLeaderboardBits(this, new OnLeaderboardEventArgs
                                {
                                    ChannelId = messageData10.ChannelId,
                                    TopList = messageData10.Top
                                });
                                return;
                            case LeaderBoardType.SubGiftSent:
                                EventHandler<OnLeaderboardEventArgs> onLeaderboardSubs = OnLeaderboardSubs;
                                if (onLeaderboardSubs == null)
                                    return;
                                onLeaderboardSubs(this, new OnLeaderboardEventArgs
                                {
                                    ChannelId = messageData10.ChannelId,
                                    TopList = messageData10.Top
                                });
                                return;
                            default:
                                return;
                        }
                    case "predictions-channel-v1":
                        PredictionEvents messageData11 = (PredictionEvents)message1.MessageData;
                        PredictionType? nullable3 = messageData11.Type;
                        if (nullable3.HasValue)
                        {
                            switch (nullable3.GetValueOrDefault())
                            {
                                case PredictionType.EventCreated:
                                    EventHandler<PredictionArgs> onPrediction1 = OnPrediction;
                                    if (onPrediction1 == null)
                                        return;
                                    onPrediction1(this, new PredictionArgs
                                    {
                                        CreatedAt = messageData11.CreatedAt,
                                        Title = messageData11.Title,
                                        ChannelId = messageData11.ChannelId,
                                        EndedAt = messageData11.EndedAt,
                                        Id = messageData11.Id,
                                        Outcomes = messageData11.Outcomes,
                                        LockedAt = messageData11.LockedAt,
                                        PredictionTime = messageData11.PredictionTime,
                                        Status = messageData11.Status,
                                        WinningOutcomeId = messageData11.WinningOutcomeId,
                                        Type = messageData11.Type
                                    });
                                    return;
                                case PredictionType.EventUpdated:
                                    EventHandler<PredictionArgs> onPrediction2 = OnPrediction;
                                    if (onPrediction2 == null)
                                        return;
                                    onPrediction2(this, new PredictionArgs
                                    {
                                        CreatedAt = messageData11.CreatedAt,
                                        Title = messageData11.Title,
                                        ChannelId = messageData11.ChannelId,
                                        EndedAt = messageData11.EndedAt,
                                        Id = messageData11.Id,
                                        Outcomes = messageData11.Outcomes,
                                        LockedAt = messageData11.LockedAt,
                                        PredictionTime = messageData11.PredictionTime,
                                        Status = messageData11.Status,
                                        WinningOutcomeId = messageData11.WinningOutcomeId,
                                        Type = messageData11.Type
                                    });
                                    return;
                                default:
                                    UnaccountedFor($"Prediction Type: {messageData11.Type}");
                                    return;
                            }
                        }

                        UnaccountedFor("Prediction Type: null");
                        return;
                    case "raid":
                        RaidEvents messageData12 = (RaidEvents)message1.MessageData;
                        RaidType? nullable4 = messageData12.Type;
                        if (!nullable4.HasValue)
                            return;
                        switch (nullable4.GetValueOrDefault())
                        {
                            case RaidType.RaidUpdate:
                                EventHandler<OnRaidUpdateArgs> onRaidUpdate = OnRaidUpdate;
                                if (onRaidUpdate == null)
                                    return;
                                onRaidUpdate(this, new OnRaidUpdateArgs
                                {
                                    Id = messageData12.Id,
                                    ChannelId = messageData12.ChannelId,
                                    TargetChannelId = messageData12.TargetChannelId,
                                    AnnounceTime = messageData12.AnnounceTime,
                                    RaidTime = messageData12.RaidTime,
                                    RemainingDurationSeconds = messageData12.RemainingDurationSeconds,
                                    ViewerCount = messageData12.ViewerCount
                                });
                                return;
                            case RaidType.RaidUpdateV2:
                                EventHandler<OnRaidUpdateV2Args> onRaidUpdateV2 = OnRaidUpdateV2;
                                if (onRaidUpdateV2 == null)
                                    return;
                                onRaidUpdateV2(this, new OnRaidUpdateV2Args
                                {
                                    Id = messageData12.Id,
                                    ChannelId = messageData12.ChannelId,
                                    TargetChannelId = messageData12.TargetChannelId,
                                    TargetLogin = messageData12.TargetLogin,
                                    TargetDisplayName = messageData12.TargetDisplayName,
                                    TargetProfileImage = messageData12.TargetProfileImage,
                                    ViewerCount = messageData12.ViewerCount
                                });
                                return;
                            case RaidType.RaidGo:
                                EventHandler<OnRaidGoArgs> onRaidGo = OnRaidGo;
                                if (onRaidGo == null)
                                    return;
                                onRaidGo(this, new OnRaidGoArgs
                                {
                                    Id = messageData12.Id,
                                    ChannelId = messageData12.ChannelId,
                                    TargetChannelId = messageData12.TargetChannelId,
                                    TargetLogin = messageData12.TargetLogin,
                                    TargetDisplayName = messageData12.TargetDisplayName,
                                    TargetProfileImage = messageData12.TargetProfileImage,
                                    ViewerCount = messageData12.ViewerCount
                                });
                                return;
                            default:
                                return;
                        }
                    case "user-moderation-notifications":
                        UserModerationNotifications messageData13 =
                                message1.MessageData as UserModerationNotifications;
                        if (messageData13.Type != UserModerationNotificationsType.AutomodCaughtMessage)
                            return;
                        AutomodCaughtResponseMessage data3 = messageData13.Data as AutomodCaughtResponseMessage;
                        EventHandler<AutomodCaughtUserMessage> caughtUserMessage =
                            OnAutomodCaughtUserMessage;
                        if (caughtUserMessage == null)
                            return;
                        caughtUserMessage(this, new AutomodCaughtUserMessage
                        {
                            ChannelId = str2,
                            UserId = message1.Topic.Split('.')[2],
                            AutomodCaughtMessage = data3
                        });
                        return;
                    case "video-playback-by-id":
                        VideoPlayback messageData14 = (VideoPlayback) message1.MessageData;
                        VideoPlaybackType? nullable5 = messageData14.Type;
                        switch (nullable5.GetValueOrDefault())
                        {
                            case VideoPlaybackType.StreamUp:
                                EventHandler<OnStreamUpArgs> onStreamUp = OnStreamUp;
                                onStreamUp(this, new OnStreamUpArgs
                                {
                                    PlayDelay = messageData14.PlayDelay,
                                    ServerTime = messageData14.ServerTime,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.StreamDown:
                                EventHandler<OnStreamDownArgs> onStreamDown = OnStreamDown;
                                onStreamDown(this, new OnStreamDownArgs
                                {
                                    ServerTime = messageData14.ServerTime,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.ViewCount:
                                EventHandler<OnViewCountArgs> onViewCount = OnViewCount;
                                onViewCount(this, new OnViewCountArgs
                                {
                                    ServerTime = messageData14.ServerTime,
                                    Viewers = messageData14.Viewers,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.Commercial:
                                EventHandler<OnCommercialArgs> onCommercial = OnCommercial;
                                onCommercial(this, new OnCommercialArgs
                                {
                                    ServerTime = messageData14.ServerTime,
                                    Length = messageData14.Length,
                                    ChannelId = str2
                                });
                                return;
                        }

                        break;
                    case "whispers":
                        Whisper messageData15 = (Whisper)message1.MessageData;
                        EventHandler<WhisperArgs> onWhisper = OnWhisper;
                        onWhisper(this, new WhisperArgs
                        {
                            Whisper = messageData15,
                            ChannelId = str2
                        });
                        return;
                }

                break;
            case "pong":
                _pongReceived = true;
                return;
            case "reconnect":
                _socket.Close();
                break;
        }

        UnaccountedFor(message);
    }

    private static string GenerateNonce()
    {
        return new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8)
            .Select((Func<string, char>)(s => s[Random.Next(s.Length)])).ToArray());
    }

    private void ListenToTopic(string topic) => _topicList.Add(topic);

    private void ListenToTopics(params string[] topics)
    {
        foreach (string topic in topics)
            _topicList.Add(topic);
    }

    public void SendTopics(string oauth = "", bool unlisten = false)
    {
        if (oauth.Contains("oauth:"))
            oauth = oauth.Replace("oauth:", "");
        string nonce = GenerateNonce();
        JArray content = new JArray();
        _previousRequestsSemaphore.WaitOne();
        try
        {
            foreach (string topic in _topicList)
            {
                _previousRequests.Add(new PreviousRequest(nonce, PubSubRequestType.ListenToTopic, topic));
                content.Add(new JValue(topic));
            }
        }
        finally
        {
            _previousRequestsSemaphore.Release();
        }

        JObject jobject = new JObject(new JProperty("type", !unlisten ? "LISTEN" : (object)"UNLISTEN"), new JProperty("nonce", nonce), new JProperty("data", new JObject(new JProperty("topics", content))));
        {
            JContainer data = (JContainer)jobject.SelectToken("data") ?? new JConstructor();
            data.Add(new JProperty("auth_token", oauth));
        }
        _socket.Send(jobject.ToString());
        _topicList.Clear();
    }

    private void UnaccountedFor(string message)
    {
        _logger.LogInformation("[TwitchPubSub] " + message);
    }

    public void ListenToFollows(string channelId)
    {
        string str = "following." + channelId;
        _topicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToChatModeratorActions(string userId, string channelId)
    {
        string str = "chat_moderator_actions." + userId + "." + channelId;
        _topicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToUserModerationNotifications(string myTwitchId, string channelTwitchId)
    {
        string str = "user-moderation-notifications." + myTwitchId + "." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToAutomodQueue(string userTwitchId, string channelTwitchId)
    {
        string str = "automod-queue." + userTwitchId + "." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToChannelExtensionBroadcast(string channelId, string extensionId)
    {
        string str = "channel-ext-v1." + channelId + "-" + extensionId + "-broadcast";
        _topicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    [Obsolete("This topic is deprecated by Twitch. Please use ListenToBitsEventsV2()", false)]
    public void ListenToBitsEvents(string channelTwitchId)
    {
        string str = "channel-bits-events-v1." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToBitsEventsV2(string channelTwitchId)
    {
        string str = "channel-bits-events-v2." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToVideoPlayback(string channelTwitchId)
    {
        string str = "video-playback-by-id." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToWhispers(string channelTwitchId)
    {
        string str = "whispers." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    [Obsolete("This method listens to an undocumented/retired/obsolete topic. Consider using ListenToChannelPoints()", false)]
    public void ListenToRewards(string channelTwitchId)
    {
        string str = "community-points-channel-v1." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToChannelPoints(string channelTwitchId)
    {
        string str = "channel-points-channel-v1." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToLeaderboards(string channelTwitchId)
    {
        string key1 = "leaderboard-events-v1.bits-usage-by-channel-v1-" + channelTwitchId + "-WEEK";
        string key2 = "leaderboard-events-v1.sub-gift-sent-" + channelTwitchId + "-WEEK";
        _topicToChannelId[key1] = channelTwitchId;
        _topicToChannelId[key2] = channelTwitchId;
        ListenToTopics(key1, key2);
    }

    public void ListenToRaid(string channelTwitchId)
    {
        string str = "raid." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToSubscriptions(string channelId)
    {
        string str = "channel-subscribe-events-v1." + channelId;
        _topicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToPredictions(string channelTwitchId)
    {
        string str = "predictions-channel-v1." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void Connect() => _socket.Open();

    public void Disconnect() => _socket.Close();

    public void TestMessageParser(string testJsonString) => ParseMessage(testJsonString);
}