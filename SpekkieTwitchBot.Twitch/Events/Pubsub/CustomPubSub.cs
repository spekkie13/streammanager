using System.Timers;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;
using SpekkieClassLibrary.Twitch.Pubsub.Types;
using SpekkieTwitchBot.General.FileHandling;
using TwitchAuthService.Events.Interfaces;
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
using OnLogArgs = SpekkieClassLibrary.Twitch.Pubsub.EventsArgs.OnLogArgs;
using RewardRedeemed = SpekkieClassLibrary.Twitch.Pubsub.Types.RewardRedeemed;
using PredictionEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.PredictionEvents;
using PredictionType = SpekkieClassLibrary.Twitch.Pubsub.Enums.PredictionType;
using RaidEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.RaidEvents;
using RaidType = SpekkieClassLibrary.Twitch.Pubsub.Enums.RaidType;
using Redemption = SpekkieClassLibrary.Twitch.Pubsub.Types.Redemption;
using Timer = System.Timers.Timer;
using UserModerationNotifications = SpekkieClassLibrary.Twitch.Pubsub.EventData.UserModerationNotifications;
using VideoPlayback = SpekkieClassLibrary.Twitch.Pubsub.Types.VideoPlayback;
using VideoPlaybackType = SpekkieClassLibrary.Twitch.Pubsub.Enums.VideoPlaybackType;
using Whisper = SpekkieClassLibrary.Twitch.Pubsub.EventData.Whisper;
using OnRaidUpdateArgs = SpekkieClassLibrary.Twitch.Pubsub.EventsArgs.OnRaidUpdateArgs;

namespace TwitchAuthService.Events.Pubsub;

public class CustomPubsub : ITwitchPubSub
{
    private static readonly Random Random = new();
    private readonly Logger _Logger;
    private readonly Timer _PingTimer = new();
    private readonly Timer _PongTimer = new();
    private readonly List<PreviousRequest> _PreviousRequests = [];
    private readonly Semaphore _PreviousRequestsSemaphore = new(1, 1);
    private readonly CustomWebSocketClient _Socket;
    private readonly List<string> _TopicList = [];
    private readonly Dictionary<string, string> _TopicToChannelId = new();
    private bool _PongReceived;

    public CustomPubsub(Logger? logger = null)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Socket = new CustomWebSocketClient(logger, new ClientOptions
        {
            ClientType = ClientType.PubSub
            
        });
        _Socket.OnConnected += Socket_OnConnected;
        _Socket.OnError += OnError;
        _Socket.OnMessage += OnMessage;
        _Socket.OnDisconnected += Socket_OnDisconnected;
        _Socket.DefaultKeepAliveInterval = TimeSpan.FromSeconds(30);
        _PongTimer.Interval = 15000.0;
        _PongTimer.Elapsed += PongTimerTick;
    }

    public event EventHandler OnPubSubServiceConnected = null!;
    public event EventHandler<OnPubSubServiceErrorArgs> OnPubSubServiceError = null!;
    public event EventHandler OnPubSubServiceClosed = null!;
    public event EventHandler<ListenResponseArgs> OnListenResponse = null!;
    public event EventHandler<OnTimeoutArgs> OnTimeout = null!;
    public event EventHandler<OnBanArgs> OnBan = null!;
    public event EventHandler<OnMessageDeletedArgs> OnMessageDeleted = null!;
    public event EventHandler<OnUnbanArgs> OnUnban = null!;
    public event EventHandler<OnUntimeoutArgs> OnUntimeout = null!;
    public event EventHandler<OnHostArgs> OnHost = null!;
    public event EventHandler<OnSubscribersOnlyArgs> OnSubscribersOnly = null!;
    public event EventHandler<OnSubscribersOnlyOffArgs> OnSubscribersOnlyOff = null!;
    public event EventHandler<OnClearArgs> OnClear = null!;
    public event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly = null!;
    public event EventHandler<OnEmoteOnlyOffArgs> OnEmoteOnlyOff = null!;
    public event EventHandler<OnBitsReceivedArgs> OnBitsReceived = null!;
    public event EventHandler<OnStreamUpArgs> OnStreamUp = null!;
    public event EventHandler<OnStreamDownArgs> OnStreamDown = null!;
    public event EventHandler<OnViewCountArgs> OnViewCount = null!;
    public event EventHandler<WhisperArgs> OnWhisper = null!;
    public event EventHandler<ChannelSubscriptionArgs> OnChannelSubscription = null!;
    public event EventHandler<OnChannelExtensionBroadcastArgs> OnChannelExtensionBroadcast = null!;
    public event EventHandler<OnFollowArgs> OnFollow = null!;
    public event EventHandler<ChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed = null!;
    public event EventHandler<OnLeaderboardEventArgs> OnLeaderboardSubs = null!;
    public event EventHandler<OnLeaderboardEventArgs> OnLeaderboardBits = null!;
    public event EventHandler<OnRaidUpdateArgs> OnRaidUpdate = null!;
    public event EventHandler<OnRaidUpdateV2Args> OnRaidUpdateV2 = null!;
    public event EventHandler<OnRaidGoArgs> OnRaidGo = null!;
    public event EventHandler<OnLogArgs> OnLog = null!;
    public event EventHandler<OnCommercialArgs> OnCommercial = null!;
    public event EventHandler<PredictionArgs> OnPrediction = null!;
    public event EventHandler<OnR9kBetaArgs> OnR9KBeta = null!;
    public event EventHandler<OnR9kBetaOffArgs> OnR9KBetaOff = null!;
    public event EventHandler<BitsReceivedV2Args> OnBitsReceivedV2 = null!;
    public event EventHandler<AutomodCaughtMessageArgs> OnAutomodCaughtMessage = null!;
    public event EventHandler<AutomodCaughtUserMessage> OnAutomodCaughtUserMessage = null!;

    public void SendTopics(string? oauth = "", bool unlisten = false)
    {
        if (oauth != null)
        {
            if (oauth.Contains("oauth:"))
                oauth = oauth.Replace("oauth:", "");
            string nonce = GenerateNonce();
            JArray content = new JArray();
            _PreviousRequestsSemaphore.WaitOne();
            try
            {
                foreach (string topic in _TopicList)
                {
                    _PreviousRequests.Add(new PreviousRequest(nonce, PubSubRequestType.ListenToTopic, topic));
                    content.Add(new JValue(topic));
                }
            }
            finally
            {
                _PreviousRequestsSemaphore.Release();
            }
            
            JObject jobject = new JObject(new JProperty("type", !unlisten ? "LISTEN" : (object)"UNLISTEN"),
                new JProperty("nonce", nonce), new JProperty("data", new JObject(new JProperty("topics", content))));
            {
                JContainer data = (JContainer?)jobject.SelectToken("data") ?? new JConstructor();
                data.Add(new JProperty("auth_token", oauth));
            }
            _Socket.Send(jobject.ToString());
            _TopicList.Clear();
        }

        
    }

    public void ListenToFollows(string channelId)
    {
        string str = "following." + channelId;
        _TopicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToChatModeratorActions(string userId, string channelId)
    {
        string str = "chat_moderator_actions." + userId + "." + channelId;
        _TopicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToChannelExtensionBroadcast(string channelId, string extensionId)
    {
        string str = "channel-ext-v1." + channelId + "-" + extensionId + "-broadcast";
        _TopicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToVideoPlayback(string channelTwitchId)
    {
        string str = "video-playback-by-id." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToWhispers(string channelTwitchId)
    {
        string str = "whispers." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }
    
    public void ListenToChannelPoints(string channelTwitchId)
    {
        string str = "channel-points-channel-v1." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToLeaderboards(string channelTwitchId)
    {
        string key1 = "leaderboard-events-v1.bits-usage-by-channel-v1-" + channelTwitchId + "-WEEK";
        string key2 = "leaderboard-events-v1.sub-gift-sent-" + channelTwitchId + "-WEEK";
        _TopicToChannelId[key1] = channelTwitchId;
        _TopicToChannelId[key2] = channelTwitchId;
        ListenToTopics(key1, key2);
    }

    public void ListenToRaid(string channelTwitchId)
    {
        string str = "raid." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void ListenToSubscriptions(string channelId)
    {
        string str = "channel-subscribe-events-v1." + channelId;
        _TopicToChannelId[str] = channelId;
        ListenToTopic(str);
    }

    public void ListenToPredictions(string channelTwitchId)
    {
        string str = "predictions-channel-v1." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }

    public void Connect()
    {
        _Socket.Open();
    }

    public void Disconnect()
    {
        _Socket.Close(false);
    }

    public void TestMessageParser(string testJsonString)
    {
        ParseMessage(testJsonString);
    }

    private void OnError(object? sender, OnErrorEventArgs e)
    {
        _Logger.LogError($"OnError in PubSub Websocket connection occured! Exception: {e.Exception}");
        EventHandler<OnPubSubServiceErrorArgs> pubSubServiceError = OnPubSubServiceError;
        pubSubServiceError(this, new OnPubSubServiceErrorArgs
        {
            Exception = e.Exception
        });
    }

    private void OnMessage(object? sender, OnMessageEventArgs e)
    {
        _Logger.LogInfo("Received Websocket OnMessage: " + e.Message);
        EventHandler<OnLogArgs> onLog = OnLog;
        onLog.Invoke(this, new OnLogArgs
        {
            Data = e.Message
        });
        ParseMessage(e.Message);
    }

    private void Socket_OnDisconnected(object? sender, EventArgs e)
    {
        _Logger.LogWarning("PubSub Websocket connection closed");
        Connect();
        _PingTimer.Stop();
        _PongTimer.Stop();
        EventHandler subServiceClosed = OnPubSubServiceClosed;
        subServiceClosed(this, EventArgs.Empty);
    }

    private void Socket_OnConnected(object? sender, EventArgs e)
    {
        _Logger.LogInfo("PubSub Websocket connection established");
        _PingTimer.Interval = 180000.0;
        _PingTimer.Elapsed += PingTimerTick;
        _PingTimer.Start();
        EventHandler serviceConnected = OnPubSubServiceConnected;
        serviceConnected(this, EventArgs.Empty);
    }

    private void PingTimerTick(object? sender, ElapsedEventArgs e)
    {
        _PongReceived = false;
        _Socket.Send(new JObject(new JProperty("type", "PING")).ToString());
        _PongTimer.Start();
    }

    private void PongTimerTick(object? sender, ElapsedEventArgs e)
    {
        _PongTimer.Stop();
        if (_PongReceived)
            _PongReceived = false;
        else
            _Socket.Close();
    }

    private void ParseMessage(string message)
    {
        string type = JObject.Parse(message).SelectToken("type")?.ToString().ToLower() ?? "";
        if (string.IsNullOrEmpty(type)) return;
        switch (type)
        {
            case "response":
                Response response = new Response(message);
                if (_PreviousRequests.Count != 0)
                {
                    bool flag = false;
                    _PreviousRequestsSemaphore.WaitOne();
                    try
                    {
                        int index = 0;
                        while (index < _PreviousRequests.Count)
                        {
                            PreviousRequest previousRequest = _PreviousRequests[index];
                            if (string.Equals(previousRequest.Nonce, response.Nonce, StringComparison.CurrentCulture))
                            {
                                _PreviousRequests.RemoveAt(index);
                                _TopicToChannelId.TryGetValue(previousRequest.Topic, out string? str);
                                EventHandler<ListenResponseArgs> onListenResponse = OnListenResponse;
                                onListenResponse.Invoke(this, new ListenResponseArgs
                                {
                                    Response = response,
                                    Topic = previousRequest.Topic,
                                    Successful = response.Successful,
                                    ChannelId = str ?? TwitchConstants.BroadcasterId
                                });
                                flag = true;
                            }
                            else
                            {
                                ++index;
                            }
                        }
                    }
                    finally
                    {
                        _PreviousRequestsSemaphore.Release();
                    }

                    if (flag)
                        return;
                }

                break;
            case nameof(message):
                Message message1 = new Message(message);
                if (!string.IsNullOrEmpty(message1.Topic))
                {
                    _TopicToChannelId.TryGetValue(message1.Topic, out string? str1);
                string str2 = str1 ?? "";
                switch (message1.Topic.Split('.')[0])
                {
                    case "automod-queue":
                        AutomodQueue? messageData1 = (AutomodQueue?) message1.MessageData;
                        switch (messageData1?.Type)
                        {
                            case AutomodQueueType.CaughtMessage:
                                AutomodCaughtMessage? data1 = messageData1.Data as AutomodCaughtMessage;
                                EventHandler<AutomodCaughtMessageArgs> automodCaughtMessage = OnAutomodCaughtMessage;
                                automodCaughtMessage(this, new AutomodCaughtMessageArgs
                                {
                                    ChannelId = str2,
                                    AutomodCaughtMessage = data1 ?? AutomodCaughtMessage.Empty,
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
                        ChannelExtensionBroadcast? messageData4 = (ChannelExtensionBroadcast?) message1.MessageData;
                        EventHandler<OnChannelExtensionBroadcastArgs> extensionBroadcast =
                            OnChannelExtensionBroadcast;
                        
                        extensionBroadcast(this, new OnChannelExtensionBroadcastArgs
                        {
                            Messages = messageData4?.Messages,
                            ChannelId = str2
                        });
                        return;
                    case "channel-points-channel-v1":
                        ChannelPointsChannel? messageData5 = (ChannelPointsChannel?) message1.MessageData;
                        switch (messageData5?.Type)
                        {
                            case ChannelPointsChannelType.RewardRedeemed:
                                RewardRedeemed? data2 = (RewardRedeemed?) messageData5.Data;
                                EventHandler<ChannelPointsRewardRedeemedArgs> pointsRewardRedeemed =
                                    OnChannelPointsRewardRedeemed;
                                
                                pointsRewardRedeemed(this, new ChannelPointsRewardRedeemedArgs
                                {
                                    ChannelId = data2?.Redemption?.ChannelId,
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
                        ChannelSubscription? messageData6 = message1.MessageData as ChannelSubscription;
                        EventHandler<ChannelSubscriptionArgs> channelSubscription = OnChannelSubscription;
                        
                        channelSubscription(this, new ChannelSubscriptionArgs
                        {
                            Subscription = messageData6 ?? ChannelSubscription.Empty,
                            ChannelId = str2
                        });
                        return;
                    case "chat_moderator_actions":
                        ChatModeratorActions? messageData7 = message1.MessageData as ChatModeratorActions;
                        string str3 = "";
                        switch (messageData7?.ModerationAction.ToLower())
                        {
                            case "ban":
                                if (messageData7.Args.Count > 1)
                                    str3 = messageData7.Args[1];
                                EventHandler<OnBanArgs> onBan = OnBan;
                                
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
                                
                                onClear(this, new OnClearArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "delete":
                                EventHandler<OnMessageDeletedArgs> onMessageDeleted = OnMessageDeleted;
                                
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
                                
                                onEmoteOnly(this, new OnEmoteOnlyArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "emoteonlyoff":
                                EventHandler<OnEmoteOnlyOffArgs> onEmoteOnlyOff = OnEmoteOnlyOff;
                                
                                onEmoteOnlyOff(this, new OnEmoteOnlyOffArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "host":
                                EventHandler<OnHostArgs> onHost = OnHost;
                                
                                onHost(this, new OnHostArgs
                                {
                                    HostedChannel = messageData7.Args[0],
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "r9kbeta":
                                EventHandler<OnR9kBetaArgs> onR9KBeta = OnR9KBeta;
                                
                                onR9KBeta(this, new OnR9kBetaArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "r9kbetaoff":
                                EventHandler<OnR9kBetaOffArgs> onR9KBetaOff = OnR9KBetaOff;
                                
                                onR9KBetaOff(this, new OnR9kBetaOffArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "subscribers":
                                EventHandler<OnSubscribersOnlyArgs> onSubscribersOnly = OnSubscribersOnly;
                                
                                onSubscribersOnly(this, new OnSubscribersOnlyArgs
                                {
                                    Moderator = messageData7.CreatedBy,
                                    ChannelId = str2
                                });
                                return;
                            case "subscribersoff":
                                EventHandler<OnSubscribersOnlyOffArgs> subscribersOnlyOff = OnSubscribersOnlyOff;

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
                        CommunityPointsChannel? messageData8 = (CommunityPointsChannel?) message1.MessageData;
                        CommunityPointsChannelType? nullable1 = messageData8?.Type;
                        switch (nullable1.GetValueOrDefault())
                        {
                            case CommunityPointsChannelType.RewardRedeemed:
                                EventHandler<ChannelPointsRewardRedeemedArgs> onRewardRedeemed = OnChannelPointsRewardRedeemed;

                                Redemption redemption = new()
                                {
                                    Id = messageData8?.RedemptionId.ToString(),
                                    User = new User{ DisplayName = messageData8?.DisplayName, Login = messageData8?.Login },
                                    ChannelId = "",
                                    RedeemedAt = messageData8?.TimeStamp ?? DateTime.MaxValue,
                                    Reward = new Reward{ Id = messageData8?.RewardId.ToString(), Cost = messageData8?.RewardCost ?? int.MaxValue, Title = messageData8?.RewardTitle, Prompt = messageData8?.RewardPrompt},
                                    UserInput = messageData8?.Message,
                                    Status = messageData8?.Status,
                                };

                                RewardRedeemed rewardRedeemed = new()
                                {
                                    Timestamp = messageData8?.TimeStamp,
                                    Redemption = redemption
                                };
                                
                                onRewardRedeemed(this, new ChannelPointsRewardRedeemedArgs
                                {
                                    ChannelId = messageData8?.ChannelId,
                                    RewardRedeemed = rewardRedeemed,
                                });
                                return;
                            case CommunityPointsChannelType.CustomRewardUpdated:
                            case CommunityPointsChannelType.CustomRewardCreated:
                            case CommunityPointsChannelType.CustomRewardDeleted:
                            default:
                                return;
                        }
                    case "following":
                        Following? messageData9 = (Following?) message1.MessageData;
                        if (messageData9 == null) return;
                        messageData9.FollowedChannelId = message1.Topic.Split('.')[1];
                        EventHandler<OnFollowArgs> onFollow = OnFollow;

                        onFollow(this, new OnFollowArgs
                        {
                            FollowedChannelId = messageData9.FollowedChannelId,
                            DisplayName = messageData9.DisplayName,
                            UserId = messageData9.UserId,
                            Username = messageData9.Username
                        });
                        return;
                    case "leaderboard-events-v1":
                        LeaderboardEvents? messageData10 = (LeaderboardEvents?) message1.MessageData;
                        LeaderBoardType? nullable2 = messageData10?.Type;
                        switch (nullable2.GetValueOrDefault())
                        {
                            case LeaderBoardType.BitsUsageByChannel:
                                EventHandler<OnLeaderboardEventArgs> onLeaderboardBits = OnLeaderboardBits;
                                
                                onLeaderboardBits(this, new OnLeaderboardEventArgs
                                {
                                    ChannelId = messageData10?.ChannelId,
                                    TopList = messageData10?.Top
                                });
                                return;
                            case LeaderBoardType.SubGiftSent:
                                EventHandler<OnLeaderboardEventArgs> onLeaderboardSubs = OnLeaderboardSubs;
                                
                                onLeaderboardSubs(this, new OnLeaderboardEventArgs
                                {
                                    ChannelId = messageData10?.ChannelId,
                                    TopList = messageData10?.Top
                                });
                                return;
                            default:
                                return;
                        }
                    case "predictions-channel-v1":
                        PredictionEvents? messageData11 = (PredictionEvents?) message1.MessageData;
                        PredictionType? nullable3 = messageData11?.Type;
                        switch (nullable3.GetValueOrDefault())
                        {
                            case PredictionType.EventCreated:
                                EventHandler<PredictionArgs> onPrediction1 = OnPrediction;
                                
                                onPrediction1(this, new PredictionArgs
                                {
                                    CreatedAt = messageData11?.CreatedAt,
                                    Title = messageData11?.Title,
                                    ChannelId = messageData11?.ChannelId,
                                    EndedAt = messageData11?.EndedAt,
                                    Id = messageData11?.Id,
                                    Outcomes = messageData11?.Outcomes,
                                    LockedAt = messageData11?.LockedAt,
                                    PredictionTime = messageData11?.PredictionTime,
                                    Status = messageData11?.Status,
                                    WinningOutcomeId = messageData11?.WinningOutcomeId,
                                    Type = messageData11?.Type
                                });
                                return;
                            case PredictionType.EventUpdated:
                                EventHandler<PredictionArgs> onPrediction2 = OnPrediction;
                                
                                onPrediction2(this, new PredictionArgs
                                {
                                    CreatedAt = messageData11?.CreatedAt,
                                    Title = messageData11?.Title,
                                    ChannelId = messageData11?.ChannelId,
                                    EndedAt = messageData11?.EndedAt,
                                    Id = messageData11?.Id,
                                    Outcomes = messageData11?.Outcomes,
                                    LockedAt = messageData11?.LockedAt,
                                    PredictionTime = messageData11?.PredictionTime,
                                    Status = messageData11?.Status,
                                    WinningOutcomeId = messageData11?.WinningOutcomeId,
                                    Type = messageData11?.Type
                                });
                                return;
                            default:
                                UnaccountedFor($"Prediction Type: {messageData11?.Type}");
                                return;
                        }

                    case "raid":
                        RaidEvents? messageData12 = (RaidEvents?) message1.MessageData;
                        RaidType? nullable4 = messageData12?.Type;
                        
                        switch (nullable4.GetValueOrDefault())
                        {
                            case RaidType.RaidUpdate:
                                EventHandler<OnRaidUpdateArgs> onRaidUpdate = OnRaidUpdate;

                                onRaidUpdate(this, new OnRaidUpdateArgs
                                {
                                    Id = messageData12?.Id,
                                    ChannelId = messageData12?.ChannelId,
                                    TargetChannelId = messageData12?.TargetChannelId,
                                    AnnounceTime = messageData12?.AnnounceTime ??  DateTime.Now,
                                    RaidTime = messageData12?.RaidTime ?? DateTime.Now,
                                    RemainingDurationSeconds = messageData12?.RemainingDurationSeconds ?? 0,
                                    ViewerCount = messageData12?.ViewerCount ?? 0
                                });
                                return;
                            case RaidType.RaidUpdateV2:
                                EventHandler<OnRaidUpdateV2Args> onRaidUpdateV2 = OnRaidUpdateV2;
                                
                                onRaidUpdateV2(this, new OnRaidUpdateV2Args
                                {
                                    Id = messageData12?.Id ?? Guid.Empty,
                                    ChannelId = messageData12?.ChannelId,
                                    TargetChannelId = messageData12?.TargetChannelId,
                                    TargetLogin = messageData12?.TargetLogin,
                                    TargetDisplayName = messageData12?.TargetDisplayName,
                                    TargetProfileImage = messageData12?.TargetProfileImage,
                                    ViewerCount = messageData12?.ViewerCount ?? 0
                                });
                                return;
                            case RaidType.RaidGo:
                                EventHandler<OnRaidGoArgs> onRaidGo = OnRaidGo;
                                
                                onRaidGo(this, new OnRaidGoArgs
                                {
                                    Id = messageData12?.Id ?? Guid.Empty,
                                    ChannelId = messageData12?.ChannelId,
                                    TargetChannelId = messageData12?.TargetChannelId,
                                    TargetLogin = messageData12?.TargetLogin,
                                    TargetDisplayName = messageData12?.TargetDisplayName,
                                    TargetProfileImage = messageData12?.TargetProfileImage,
                                    ViewerCount = messageData12?.ViewerCount ?? 0
                                });
                                return;
                            default:
                                return;
                        }
                    case "user-moderation-notifications":
                        UserModerationNotifications? messageData13 = message1.MessageData as UserModerationNotifications;
                        if (messageData13?.Type != UserModerationNotificationsType.AutomodCaughtMessage)
                            return;
                        AutomodCaughtResponseMessage? data3 = messageData13.Data as AutomodCaughtResponseMessage;
                        EventHandler<AutomodCaughtUserMessage> caughtUserMessage = OnAutomodCaughtUserMessage;

                        caughtUserMessage(this, new AutomodCaughtUserMessage
                        {
                            ChannelId = str2,
                            UserId = message1.Topic.Split('.')[2],
                            AutomodCaughtMessage = data3 ?? AutomodCaughtResponseMessage.Empty,
                        });
                        return;
                    case "video-playback-by-id":
                        VideoPlayback? messageData14 = (VideoPlayback?) message1.MessageData;
                        VideoPlaybackType? nullable5 = messageData14?.Type;
                        switch (nullable5.GetValueOrDefault())
                        {
                            case VideoPlaybackType.StreamUp:
                                EventHandler<OnStreamUpArgs> onStreamUp = OnStreamUp;
                                onStreamUp(this, new OnStreamUpArgs
                                {
                                    PlayDelay = messageData14?.PlayDelay ?? int.MaxValue,
                                    ServerTime = messageData14?.ServerTime,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.StreamDown:
                                EventHandler<OnStreamDownArgs> onStreamDown = OnStreamDown;
                                onStreamDown(this, new OnStreamDownArgs
                                {
                                    ServerTime = messageData14?.ServerTime,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.ViewCount:
                                EventHandler<OnViewCountArgs> onViewCount = OnViewCount;
                                onViewCount(this, new OnViewCountArgs
                                {
                                    ServerTime = messageData14?.ServerTime,
                                    Viewers = messageData14?.Viewers ?? 0,
                                    ChannelId = str2
                                });
                                return;
                            case VideoPlaybackType.Commercial:
                                EventHandler<OnCommercialArgs> onCommercial = OnCommercial;
                                onCommercial(this, new OnCommercialArgs
                                {
                                    ServerTime = messageData14?.ServerTime,
                                    Length = messageData14?.Length ?? int.MaxValue,
                                    ChannelId = str2
                                });
                                return;
                        }

                        break;
                    case "whispers":
                        Whisper? messageData15 = (Whisper?) message1.MessageData;
                        EventHandler<WhisperArgs> onWhisper = OnWhisper;
                        onWhisper(this, new WhisperArgs
                        {
                            Whisper = messageData15,
                            ChannelId = str2
                        });
                        return;
                    }
                }   
                

                break;
            case "pong":
                _PongReceived = true;
                return;
            case "reconnect":
                _Socket.Close();
                break;
        }

        UnaccountedFor(message);
    }

    private static string GenerateNonce()
    {
        return new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8)
            .Select((Func<string, char>)(s => s[Random.Next(s.Length)])).ToArray());
    }

    private void ListenToTopic(string topic)
    {
        _TopicList.Add(topic);
    }

    private void ListenToTopics(params string[] topics)
    {
        foreach (string topic in topics)
            _TopicList.Add(topic);
    }

    private void UnaccountedFor(string message)
    {
        _Logger.LogInfo("[TwitchPubSub] " + message);
    }

    public void ListenToBitsEventsV2(string channelTwitchId)
    {
        string str = "channel-bits-events-v2." + channelTwitchId;
        _TopicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }
}