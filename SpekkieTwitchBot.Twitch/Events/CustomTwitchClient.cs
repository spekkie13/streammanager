#nullable disable
using System.Reflection;
using System.Timers;
using SpekkieTwitchBot.General.FileHandling;
using TwitchAuthService.General;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Enums.Internal;
using TwitchLib.Client.Events;
using TwitchLib.Client.Exceptions;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Internal;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using Timer = System.Timers.Timer;

namespace TwitchAuthService.Events;

public class CustomTwitchClient(Logger logger, ClientProtocol protocol = ClientProtocol.WebSocket, IClient client = null) : ITwitchClient
{
    private readonly ICollection<char> _chatCommandIdentifiers = new HashSet<char>();
    private readonly List<string> _hasSeenJoinedChannels = [];
    private readonly IrcParser _ircParser = new();
    private readonly Queue<JoinedChannel> _joinChannelQueue = [];
    private readonly JoinedChannelManager _joinedChannelManager = new();
    private readonly ICollection<char> _whisperCommandIdentifiers = new HashSet<char>();
    private List<KeyValuePair<string, DateTime>> _awaitingJoins;
    private MessageEmoteCollection _channelEmotes = new();
    private IClient _client = client;
    private bool _currentlyJoiningChannels;
    private Timer _joinTimer;
    private string _lastMessageSent;

    public static Version Version => Assembly.GetEntryAssembly()?.GetName().Version;
    public bool IsInitialized => _client != null;
    public IReadOnlyList<JoinedChannel> JoinedChannels => _joinedChannelManager.GetJoinedChannels();
    public string TwitchUsername { get; private set; }
    public WhisperMessage PreviousWhisper { get; private set; }
    public bool IsConnected => IsInitialized && _client is { IsConnected: true };
    public MessageEmoteCollection ChannelEmotes => _channelEmotes;
    public bool DisableAutoPong { get; set; }
    public bool WillReplaceEmotes { get; set; }
    public ConnectionCredentials ConnectionCredentials { get; private set; }
    public bool AutoReListenOnException { get; set; }

    public event EventHandler<OnAnnouncementArgs> OnAnnouncement;
    public event EventHandler<OnVIPsReceivedArgs> OnVIPsReceived;
    public event EventHandler<OnLogArgs> OnLog;
    public event EventHandler<OnConnectedArgs> OnConnected;
    public event EventHandler<OnJoinedChannelArgs> OnJoinedChannel;
    public event EventHandler<OnIncorrectLoginArgs> OnIncorrectLogin;
    public event EventHandler<OnChannelStateChangedArgs> OnChannelStateChanged;
    public event EventHandler<OnUserStateChangedArgs> OnUserStateChanged;
    public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
    public event EventHandler<OnWhisperReceivedArgs> OnWhisperReceived;
    public event EventHandler<OnMessageSentArgs> OnMessageSent;
    public event EventHandler<OnWhisperSentArgs> OnWhisperSent;
    public event EventHandler<OnChatCommandReceivedArgs> OnChatCommandReceived;
    public event EventHandler<OnWhisperCommandReceivedArgs> OnWhisperCommandReceived;
    public event EventHandler<OnUserJoinedArgs> OnUserJoined;
    public event EventHandler<OnModeratorJoinedArgs> OnModeratorJoined;
    public event EventHandler<OnModeratorLeftArgs> OnModeratorLeft;
    public event EventHandler<OnMessageClearedArgs> OnMessageCleared;
    public event EventHandler<OnNewSubscriberArgs> OnNewSubscriber;
    public event EventHandler<OnReSubscriberArgs> OnReSubscriber;
    public event EventHandler<OnExistingUsersDetectedArgs> OnExistingUsersDetected;
    public event EventHandler<OnUserLeftArgs> OnUserLeft;
    public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
    public event EventHandler<OnConnectionErrorArgs> OnConnectionError;
    public event EventHandler<OnChatClearedArgs> OnChatCleared;
    public event EventHandler<OnUserTimedoutArgs> OnUserTimedout;
    public event EventHandler<OnLeftChannelArgs> OnLeftChannel;
    public event EventHandler<OnUserBannedArgs> OnUserBanned;
    public event EventHandler<OnModeratorsReceivedArgs> OnModeratorsReceived;
    public event EventHandler<OnChatColorChangedArgs> OnChatColorChanged;
    public event EventHandler<OnSendReceiveDataArgs> OnSendReceiveData;
    public event EventHandler<OnRaidNotificationArgs> OnRaidNotification;
    public event EventHandler<OnGiftedSubscriptionArgs> OnGiftedSubscription;
    public event EventHandler<OnCommunitySubscriptionArgs> OnCommunitySubscription;
    public event EventHandler<OnMessageThrottledEventArgs> OnMessageThrottled;
    public event EventHandler<OnWhisperThrottledEventArgs> OnWhisperThrottled;
    public event EventHandler<OnErrorEventArgs> OnError;
    public event EventHandler<OnReconnectedEventArgs> OnReconnected;
    public event EventHandler<OnRequiresVerifiedEmailArgs> OnRequiresVerifiedEmail;
    public event EventHandler<OnRequiresVerifiedPhoneNumberArgs> OnRequiresVerifiedPhoneNumber;
    public event EventHandler<OnBannedEmailAliasArgs> OnBannedEmailAlias;
    public event EventHandler<OnUserIntroArgs> OnUserIntro;

    public void Initialize(ConnectionCredentials credentials, string channel = null, char chatCommandIdentifier = '!', char whisperCommandIdentifier = '!', bool autoReListenOnExceptions = true)
    {
        if (channel != null && channel[0] == '#')
            channel = channel[1..];
        var channels = new List<string> { channel };
        int chatCommandIdentifier1 = chatCommandIdentifier;
        int whisperCommandIdentifier1 = whisperCommandIdentifier;
        var num = autoReListenOnExceptions ? 1 : 0;
        InitializeHelper(credentials, channels, (char)chatCommandIdentifier1, (char)whisperCommandIdentifier1,
            num != 0);
    }

    public void Initialize(ConnectionCredentials credentials, List<string> channels, char chatCommandIdentifier = '!', char whisperCommandIdentifier = '!', bool autoReListenOnExceptions = true)
    {
        channels = channels.Select((Func<string, string>)(x => x[0] != '#' ? x : x[1..]))
            .ToList();
        InitializeHelper(credentials, channels, chatCommandIdentifier, whisperCommandIdentifier,
            autoReListenOnExceptions);
    }

    public void SendRaw(string message)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        logger.LogInfo("Writing: " + message);
        _client.Send(message);
        var onSendReceiveData = OnSendReceiveData;
        if (onSendReceiveData == null)
            return;
        onSendReceiveData(this, new OnSendReceiveDataArgs
        {
            Direction = SendReceiveDirection.Sent,
            Data = message
        });
    }

    public void SendMessage(JoinedChannel channel, string message, bool dryRun = false)
    {
        SendTwitchMessage(channel, message, dryRun: dryRun);
    }

    public void SendMessage(string channel, string message, bool dryRun = false)
    {
        SendMessage(GetJoinedChannel(channel), message, dryRun);
    }

    public void SendReply(JoinedChannel channel, string replyToId, string message, bool dryRun = false)
    {
        SendTwitchMessage(channel, message, replyToId, dryRun);
    }

    public void SendReply(string channel, string replyToId, string message, bool dryRun = false)
    {
        SendReply(GetJoinedChannel(channel), replyToId, message, dryRun);
    }

    public void SendWhisper(string receiver, string message, bool dryRun = false)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        if (dryRun)
            return;
        _client.SendWhisper(new OutboundWhisperMessage
        {
            Receiver = receiver,
            Username = ConnectionCredentials.TwitchUsername,
            Message = message
        }.ToString());
        var onWhisperSent = OnWhisperSent;
        if (onWhisperSent == null)
            return;
        onWhisperSent(this, new OnWhisperSentArgs
        {
            Receiver = receiver,
            Message = message
        });
    }

    public bool Connect()
    {
        if (!IsInitialized)
            HandleNotInitialized();
        logger.LogInfo("Connecting to: " + ConnectionCredentials.TwitchWebsocketURI);
        _joinedChannelManager.Clear();
        if (!_client.Open())
            return false;
        logger.LogInfo("Should be connected!");
        return true;
    }

    public void Disconnect()
    {
        logger.LogInfo("Disconnect Twitch Chat Client...");
        if (!IsInitialized)
            HandleNotInitialized();
        _client.Close();
        _joinedChannelManager.Clear();
        PreviousWhisper = null;
    }

    public void Reconnect()
    {
        if (!IsInitialized)
            HandleNotInitialized();
        logger.LogInfo("Reconnecting to Twitch");
        _client.Reconnect();
    }

    public void AddChatCommandIdentifier(char identifier)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        _chatCommandIdentifiers.Add(identifier);
    }

    public void RemoveChatCommandIdentifier(char identifier)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        _chatCommandIdentifiers.Remove(identifier);
    }

    public void AddWhisperCommandIdentifier(char identifier)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        _whisperCommandIdentifiers.Add(identifier);
    }

    public void RemoveWhisperCommandIdentifier(char identifier)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        _whisperCommandIdentifiers.Remove(identifier);
    }

    public void SetConnectionCredentials(ConnectionCredentials credentials)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        if (IsConnected)
            throw new IllegalAssignmentException(
                "While the client is connected, you are unable to change the connection credentials. Please disconnect first and then change them.");
        ConnectionCredentials = credentials;
    }

    public void JoinChannel(string channel, bool overrideCheck = false)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        if (!IsConnected)
            HandleNotConnected();
        var channel1 = channel;
        if (JoinedChannels.FirstOrDefault(
                (Func<JoinedChannel, bool>)(x => x.Channel.ToLower() == channel1 && !overrideCheck)) != null)
            return;
        if (channel[0] == '#')
            channel = channel.Substring(1);
        _joinChannelQueue.Enqueue(new JoinedChannel(channel));
        if (_currentlyJoiningChannels)
            return;
        QueueingJoinCheck();
    }

    public JoinedChannel GetJoinedChannel(string channel)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        if (JoinedChannels.Count == 0)
            throw new BadStateException("Must be connected to at least one channel.");
        if (channel[0] == '#')
            channel = channel.Substring(1);
        return _joinedChannelManager.GetJoinedChannel(channel);
    }

    public void LeaveChannel(string channel)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        channel = channel.ToLower();
        if (channel[0] == '#')
            channel = channel.Substring(1);
        logger.LogInfo("Leaving channel: " + channel);
        if (_joinedChannelManager.GetJoinedChannel(channel) == null)
            return;
        _client.Send(Rfc2812.Part("#" + channel));
    }

    public void LeaveChannel(JoinedChannel channel)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        LeaveChannel(channel.Channel);
    }

    public void OnReadLineTest(string rawIrc)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        HandleIrcMessage(_ircParser.ParseIrcMessage(rawIrc));
    }

    public void SendQueuedItem(string message)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        _client.Send(message);
    }

    public event EventHandler<OnPrimePaidSubscriberArgs> OnPrimePaidSubscriber;
    public event EventHandler<OnContinuedGiftedSubscriptionArgs> OnContinuedGiftedSubscription;
    public event EventHandler<OnRateLimitArgs> OnRateLimit;
    public event EventHandler<OnDuplicateArgs> OnDuplicate;
    public event EventHandler OnSelfRaidError;
    public event EventHandler OnNoPermissionError;
    public event EventHandler OnRaidedChannelIsMatureAudience;
    public event EventHandler<OnFailureToReceiveJoinConfirmationArgs> OnFailureToReceiveJoinConfirmation;
    public event EventHandler<OnFollowersOnlyArgs> OnFollowersOnly;
    public event EventHandler<OnSubsOnlyArgs> OnSubsOnly;
    public event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly;
    public event EventHandler<OnSuspendedArgs> OnSuspended;
    public event EventHandler<OnBannedArgs> OnBanned;
    public event EventHandler<OnSlowModeArgs> OnSlowMode;
    public event EventHandler<OnR9kModeArgs> OnR9KMode;
    public event EventHandler<OnUnaccountedForArgs> OnUnaccountedFor;

    private void InitializeHelper(
        ConnectionCredentials credentials,
        List<string> channels,
        char chatCommandIdentifier = '!',
        char whisperCommandIdentifier = '!',
        bool autoReListenOnExceptions = true)
    {
        logger.LogInfo(
            $"CustomTwitchClient initialized, assembly version: {Assembly.GetExecutingAssembly().GetName().Version}");
        ConnectionCredentials = credentials;
        TwitchUsername = ConnectionCredentials.TwitchUsername;
        if (chatCommandIdentifier != char.MinValue)
            _chatCommandIdentifiers.Add(chatCommandIdentifier);
        if (whisperCommandIdentifier != char.MinValue)
            _whisperCommandIdentifiers.Add(whisperCommandIdentifier);
        AutoReListenOnException = autoReListenOnExceptions;
        if (channels is { Count: > 0 })
            for (var i = 0; i < channels.Count; i++)
            {
                if (string.IsNullOrEmpty(channels[i])) continue;
                var i1 = i;
                if (JoinedChannels.FirstOrDefault(
                        (Func<JoinedChannel, bool>)(x => x.Channel.ToLower() == channels[i1])) != null)
                    return;
                _joinChannelQueue.Enqueue(new JoinedChannel(channels[i]));
            }

        InitializeClient();
    }

    private void InitializeClient()
    {
        if (_client == null)
            switch (protocol)
            {
                case ClientProtocol.TCP:
                    _client = new TcpClient();
                    break;
                case ClientProtocol.WebSocket:
                    _client = new WebSocketClient();
                    break;
            }

        if (_client == null) return;

        _client.OnConnected += _client_OnConnected;
        _client.OnMessage += _client_OnMessage;
        _client.OnDisconnected += _client_OnDisconnected;
        _client.OnFatality += _client_OnFatality;
        _client.OnMessageThrottled += _client_OnMessageThrottled;
        _client.OnWhisperThrottled += _client_OnWhisperThrottled;
        _client.OnReconnected += _client_OnReconnected;
    }

    internal void RaiseEvent(string eventName, object args = null)
    {
        var invocationList = (GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(this) as MulticastDelegate)?.GetInvocationList();
        if (invocationList == null) return;

        foreach (var invocation in invocationList)
        {
            var method = invocation.Method;
            var target = invocation.Target;
            object[] parameters;
            if (args != null)
                parameters = new[] { this, args };
            else
                parameters = new object[]
                {
                    this,
                    EventArgs.Empty
                };
            method.Invoke(target, parameters);
        }
    }

    private void SendTwitchMessage(
        JoinedChannel channel,
        string message,
        string replyToId = null,
        bool dryRun = false)
    {
        if (!IsInitialized)
            HandleNotInitialized();
        if (((channel == null ? 1 : message == "" ? 1 : 0) | (dryRun ? 1 : 0)) != 0)
            return;
        if (message.Length > 500)
        {
            logger.LogError("Message length has exceeded the maximum character count. (500)");
        }
        else
        {
            var outboundChatMessage = new OutboundChatMessage
            {
                Channel = channel?.Channel,
                Username = ConnectionCredentials.TwitchUsername,
                Message = message
            };
            if (replyToId != null)
                outboundChatMessage.ReplyToId = replyToId;
            _lastMessageSent = message;
            _client.Send(outboundChatMessage.ToString());
        }
    }

    private void _client_OnWhisperThrottled(object sender, OnWhisperThrottledEventArgs e)
    {
        var whisperThrottled = OnWhisperThrottled;
        if (whisperThrottled == null)
            return;
        whisperThrottled(sender, e);
    }

    private void _client_OnMessageThrottled(object sender, OnMessageThrottledEventArgs e)
    {
        var messageThrottled = OnMessageThrottled;
        if (messageThrottled == null)
            return;
        messageThrottled(sender, e);
    }

    private void _client_OnFatality(object sender, OnFatalErrorEventArgs e)
    {
        var onConnectionError = OnConnectionError;
        if (onConnectionError == null)
            return;
        onConnectionError(this, new OnConnectionErrorArgs
        {
            BotUsername = TwitchUsername,
            Error = new ErrorEvent { Message = e.Reason }
        });
    }

    private void _client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
    {
        var onDisconnected = OnDisconnected;
        if (onDisconnected == null)
            return;
        onDisconnected(sender, e);
    }

    private void _client_OnReconnected(object sender, OnReconnectedEventArgs e)
    {
        foreach (var joinedChannel in (IEnumerable<JoinedChannel>)_joinedChannelManager
                     .GetJoinedChannels())
            if (!string.Equals(joinedChannel.Channel, TwitchUsername,
                    StringComparison.CurrentCultureIgnoreCase))
                _joinChannelQueue.Enqueue(joinedChannel);

        _joinedChannelManager.Clear();
        var onReconnected = OnReconnected;
        if (onReconnected == null)
            return;
        onReconnected(sender, e);
    }

    private void _client_OnMessage(object sender, OnMessageEventArgs e)
    {
        string[] separator = ["\r\n"];
        foreach (var raw in e.Message.Split(separator, StringSplitOptions.None))
            if (raw.Length > 1)
            {
                logger.LogInfo("Received: " + raw);
                var onSendReceiveData = OnSendReceiveData;
                if (onSendReceiveData != null)
                    onSendReceiveData(this, new OnSendReceiveDataArgs
                    {
                        Direction = SendReceiveDirection.Received,
                        Data = raw
                    });
                HandleIrcMessage(_ircParser.ParseIrcMessage(raw));
            }
    }

    private void _client_OnConnected(object sender, object e)
    {
        _client.Send(Rfc2812.Pass(ConnectionCredentials.TwitchOAuth));
        _client.Send(Rfc2812.Nick(ConnectionCredentials.TwitchUsername));
        _client.Send(Rfc2812.User(ConnectionCredentials.TwitchUsername, 0,
            ConnectionCredentials.TwitchUsername));
        if (ConnectionCredentials.Capabilities.Membership)
            _client.Send("CAP REQ twitch.tv/membership");
        if (ConnectionCredentials.Capabilities.Commands)
            _client.Send("CAP REQ twitch.tv/commands");
        if (ConnectionCredentials.Capabilities.Tags)
            _client.Send("CAP REQ twitch.tv/tags");
        if (_joinChannelQueue == null || _joinChannelQueue.Count <= 0)
            return;
        QueueingJoinCheck();
    }

    private void QueueingJoinCheck()
    {
        if (_joinChannelQueue.Count > 0)
        {
            _currentlyJoiningChannels = true;
            var joinedChannel = _joinChannelQueue.Dequeue();
            logger.LogInfo("Joining channel: " + joinedChannel.Channel);
            _client.Send(Rfc2812.Join("#" + joinedChannel.Channel.ToLower()));
            _joinedChannelManager.AddJoinedChannel(new JoinedChannel(joinedChannel.Channel));
            StartJoinedChannelTimer(joinedChannel.Channel);
        }
        else
        {
            logger.LogInfo("Finished channel joining queue.");
        }
    }

    private void StartJoinedChannelTimer(string channel)
    {
        if (_joinTimer == null)
        {
            _joinTimer = new Timer(1000.0);
            _joinTimer.Elapsed += JoinChannelTimeout;
            _awaitingJoins = new List<KeyValuePair<string, DateTime>>();
        }

        _awaitingJoins.Add(new KeyValuePair<string, DateTime>(channel.ToLower(), DateTime.Now));
        if (_joinTimer.Enabled)
            return;
        _joinTimer.Start();
    }

    private void JoinChannelTimeout(object sender, ElapsedEventArgs e)
    {
        if (_awaitingJoins.Any())
        {
            var list = _awaitingJoins
                .Where(
                    (Func<KeyValuePair<string, DateTime>, bool>)(x => (DateTime.Now - x.Value).TotalSeconds > 5.0))
                .ToList();
            if (!list.Any())
                return;
            _awaitingJoins.RemoveAll(
                (Predicate<KeyValuePair<string, DateTime>>)(x => (DateTime.Now - x.Value).TotalSeconds > 5.0));
            foreach (var keyValuePair in list)
            {
                _joinedChannelManager.RemoveJoinedChannel(keyValuePair.Key.ToLowerInvariant());
                var joinConfirmation =
                    OnFailureToReceiveJoinConfirmation;
                if (joinConfirmation != null)
                    joinConfirmation(this, new OnFailureToReceiveJoinConfirmationArgs
                    {
                        Exception = new FailureToReceiveJoinConfirmationException(keyValuePair.Key)
                    });
            }
        }
        else
        {
            _joinTimer.Stop();
            _currentlyJoiningChannels = false;
            QueueingJoinCheck();
        }
    }

    private void HandleIrcMessage(IrcMessage ircMessage)
    {
        if (ircMessage.Message.Contains("Login authentication failed"))
        {
            var onIncorrectLogin = OnIncorrectLogin;
            if (onIncorrectLogin == null)
                return;
            onIncorrectLogin(this, new OnIncorrectLoginArgs
            {
                Exception = new ErrorLoggingInException(ircMessage.ToString(), TwitchUsername)
            });
        }
        else
        {
            switch (ircMessage.Command)
            {
                case IrcCommand.PrivMsg:
                    HandlePrivMsg(ircMessage);
                    break;
                case IrcCommand.Notice:
                    HandleNotice(ircMessage);
                    break;
                case IrcCommand.Ping:
                    if (DisableAutoPong)
                        break;
                    SendRaw("PONG");
                    break;
                case IrcCommand.Pong:
                    break;
                case IrcCommand.Join:
                    HandleJoin(ircMessage);
                    break;
                case IrcCommand.Part:
                    HandlePart(ircMessage);
                    break;
                case IrcCommand.ClearChat:
                    HandleClearChat(ircMessage);
                    break;
                case IrcCommand.ClearMsg:
                    HandleClearMsg(ircMessage);
                    break;
                case IrcCommand.UserState:
                    HandleUserState(ircMessage);
                    break;
                case IrcCommand.GlobalUserState:
                    break;
                case IrcCommand.Cap:
                    HandleCap(ircMessage);
                    break;
                case IrcCommand.RPL_001:
                    break;
                case IrcCommand.RPL_002:
                    break;
                case IrcCommand.RPL_003:
                    break;
                case IrcCommand.RPL_004:
                    Handle004();
                    break;
                case IrcCommand.RPL_353:
                    Handle353(ircMessage);
                    break;
                case IrcCommand.RPL_366:
                    Handle366();
                    break;
                case IrcCommand.RPL_372:
                    break;
                case IrcCommand.RPL_375:
                    break;
                case IrcCommand.RPL_376:
                    break;
                case IrcCommand.Whisper:
                    HandleWhisper(ircMessage);
                    break;
                case IrcCommand.RoomState:
                    HandleRoomState(ircMessage);
                    break;
                case IrcCommand.Reconnect:
                    Reconnect();
                    break;
                case IrcCommand.UserNotice:
                    HandleUserNotice(ircMessage);
                    break;
                case IrcCommand.Mode:
                    HandleMode(ircMessage);
                    break;
                default:
                    var onUnaccountedFor = OnUnaccountedFor;
                    if (onUnaccountedFor != null)
                        onUnaccountedFor(this, new OnUnaccountedForArgs
                        {
                            BotUsername = TwitchUsername,
                            Channel = null,
                            Location = nameof(HandleIrcMessage),
                            RawIRC = ircMessage.ToString()
                        });
                    UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }

    private void HandlePrivMsg(IrcMessage ircMessage)
    {
        var chatMessage = new ChatMessage(TwitchUsername, ircMessage, ref _channelEmotes,
            WillReplaceEmotes);
        foreach (var joinedChannel in JoinedChannels.Where(
                     (Func<JoinedChannel, bool>)(x => string.Equals(x.Channel, ircMessage.Channel,
                         StringComparison.InvariantCultureIgnoreCase))))
            joinedChannel.HandleMessage(chatMessage);
        var onMessageReceived = OnMessageReceived;
        if (onMessageReceived != null)
            onMessageReceived(this, new OnMessageReceivedArgs
            {
                ChatMessage = chatMessage
            });
        if (ircMessage.Tags.TryGetValue("msg-id", out var str) && str == "user-intro")
        {
            var onUserIntro = OnUserIntro;
            if (onUserIntro != null)
                onUserIntro(this, new OnUserIntroArgs
                {
                    ChatMessage = chatMessage
                });
        }

        if (_chatCommandIdentifiers == null || _chatCommandIdentifiers.Count == 0 ||
            string.IsNullOrEmpty(chatMessage.Message) ||
            !_chatCommandIdentifiers.Contains(chatMessage.Message[0]))
            return;
        var chatCommand = new ChatCommand(chatMessage);
        var chatCommandReceived = OnChatCommandReceived;
        if (chatCommandReceived == null)
            return;
        chatCommandReceived(this, new OnChatCommandReceivedArgs
        {
            Command = chatCommand
        });
    }

    private void HandleNotice(IrcMessage ircMessage)
    {
        if (ircMessage.Message.Contains("Improperly formatted auth"))
        {
            var onIncorrectLogin = OnIncorrectLogin;
            if (onIncorrectLogin == null)
                return;
            onIncorrectLogin(this, new OnIncorrectLoginArgs
            {
                Exception = new ErrorLoggingInException(ircMessage.ToString(), TwitchUsername)
            });
        }
        else
        {
            if (!ircMessage.Tags.TryGetValue("msg-id", out var str))
            {
                var onUnaccountedFor = OnUnaccountedFor;
                if (onUnaccountedFor != null)
                    onUnaccountedFor(this, new OnUnaccountedForArgs
                    {
                        BotUsername = TwitchUsername,
                        Channel = ircMessage.Channel,
                        Location = "NoticeHandling",
                        RawIRC = ircMessage.ToString()
                    });
                UnaccountedFor(ircMessage.ToString());
            }

            switch (str)
            {
                case "color_changed":
                    var chatColorChanged = OnChatColorChanged;
                    if (chatColorChanged == null)
                        break;
                    chatColorChanged(this, new OnChatColorChangedArgs
                    {
                        Channel = ircMessage.Channel
                    });
                    break;
                case "msg_banned":
                    var onBanned = OnBanned;
                    if (onBanned == null)
                        break;
                    onBanned(this, new OnBannedArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_banned_email_alias":
                    var bannedEmailAlias = OnBannedEmailAlias;
                    if (bannedEmailAlias == null)
                        break;
                    bannedEmailAlias(this, new OnBannedEmailAliasArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_channel_suspended":
                    _awaitingJoins.RemoveAll(
                        (Predicate<KeyValuePair<string, DateTime>>)(x => x.Key.ToLower() == ircMessage.Channel));
                    _joinedChannelManager.RemoveJoinedChannel(ircMessage.Channel);
                    QueueingJoinCheck();
                    var joinConfirmation =
                        OnFailureToReceiveJoinConfirmation;
                    if (joinConfirmation == null)
                        break;
                    joinConfirmation(this, new OnFailureToReceiveJoinConfirmationArgs
                    {
                        Exception = new FailureToReceiveJoinConfirmationException(ircMessage.Channel,
                            ircMessage.Message)
                    });
                    break;
                case "msg_duplicate":
                    var onDuplicate = OnDuplicate;
                    if (onDuplicate == null)
                        break;
                    onDuplicate(this, new OnDuplicateArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_emoteonly":
                    var onEmoteOnly = OnEmoteOnly;
                    if (onEmoteOnly == null)
                        break;
                    onEmoteOnly(this, new OnEmoteOnlyArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_followersonly":
                    var onFollowersOnly = OnFollowersOnly;
                    if (onFollowersOnly == null)
                        break;
                    onFollowersOnly(this, new OnFollowersOnlyArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_r9k":
                    var onR9KMode = OnR9KMode;
                    if (onR9KMode == null)
                        break;
                    onR9KMode(this, new OnR9kModeArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_ratelimit":
                    var onRateLimit = OnRateLimit;
                    if (onRateLimit == null)
                        break;
                    onRateLimit(this, new OnRateLimitArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_requires_verified_phone_number":
                    var verifiedPhoneNumber =
                        OnRequiresVerifiedPhoneNumber;
                    if (verifiedPhoneNumber == null)
                        break;
                    verifiedPhoneNumber(this, new OnRequiresVerifiedPhoneNumberArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_slowmode":
                    var onSlowMode = OnSlowMode;
                    if (onSlowMode == null)
                        break;
                    onSlowMode(this, new OnSlowModeArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_subsonly":
                    var onSubsOnly = OnSubsOnly;
                    if (onSubsOnly == null)
                        break;
                    onSubsOnly(this, new OnSubsOnlyArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_suspended":
                    var onSuspended = OnSuspended;
                    if (onSuspended == null)
                        break;
                    onSuspended(this, new OnSuspendedArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_verified_email":
                    var requiresVerifiedEmail = OnRequiresVerifiedEmail;
                    if (requiresVerifiedEmail == null)
                        break;
                    requiresVerifiedEmail(this, new OnRequiresVerifiedEmailArgs
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "no_mods":
                    var moderatorsReceived1 = OnModeratorsReceived;
                    if (moderatorsReceived1 == null)
                        break;
                    moderatorsReceived1(this, new OnModeratorsReceivedArgs
                    {
                        Channel = ircMessage.Channel,
                        Moderators = new List<string>()
                    });
                    break;
                case "no_permission":
                    var noPermissionError = OnNoPermissionError;
                    if (noPermissionError == null)
                        break;
                    noPermissionError(this, EventArgs.Empty);
                    break;
                case "no_vips":
                    var onViPsReceived1 = OnVIPsReceived;
                    if (onViPsReceived1 == null)
                        break;
                    onViPsReceived1(this, new OnVIPsReceivedArgs
                    {
                        Channel = ircMessage.Channel,
                        VIPs = new List<string>()
                    });
                    break;
                case "raid_error_self":
                    var onSelfRaidError = OnSelfRaidError;
                    if (onSelfRaidError == null)
                        break;
                    onSelfRaidError(this, EventArgs.Empty);
                    break;
                case "raid_notice_mature":
                    var isMatureAudience = OnRaidedChannelIsMatureAudience;
                    if (isMatureAudience == null)
                        break;
                    isMatureAudience(this, EventArgs.Empty);
                    break;
                case "room_mods":
                    var moderatorsReceived2 = OnModeratorsReceived;
                    if (moderatorsReceived2 == null)
                        break;
                    moderatorsReceived2(this, new OnModeratorsReceivedArgs
                    {
                        Channel = ircMessage.Channel,
                        Moderators =
                            ircMessage.Message.Replace(" ", "").Split(':')[1].Split(',')
                                .ToList()
                    });
                    break;
                case "vips_success":
                    var onViPsReceived2 = OnVIPsReceived;
                    if (onViPsReceived2 == null)
                        break;
                    onViPsReceived2(this, new OnVIPsReceivedArgs
                    {
                        Channel = ircMessage.Channel,
                        VIPs =
                            ircMessage.Message.Replace(" ", "").Replace(".", "").Split(':')[1]
                                .Split(',').ToList()
                    });
                    break;
                default:
                    var onUnaccountedFor1 = OnUnaccountedFor;
                    if (onUnaccountedFor1 != null)
                        onUnaccountedFor1(this, new OnUnaccountedForArgs
                        {
                            BotUsername = TwitchUsername,
                            Channel = ircMessage.Channel,
                            Location = "NoticeHandling",
                            RawIRC = ircMessage.ToString()
                        });
                    UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }

    private void HandleJoin(IrcMessage ircMessage)
    {
        var onUserJoined = OnUserJoined;
        if (onUserJoined == null)
            return;
        onUserJoined(this, new OnUserJoinedArgs
        {
            Channel = ircMessage.Channel,
            Username = ircMessage.User
        });
    }

    private void HandlePart(IrcMessage ircMessage)
    {
        if (string.Equals(TwitchUsername, ircMessage.User, StringComparison.InvariantCultureIgnoreCase))
        {
            _joinedChannelManager.RemoveJoinedChannel(ircMessage.Channel);
            _hasSeenJoinedChannels.Remove(ircMessage.Channel);
            var onLeftChannel = OnLeftChannel;
            if (onLeftChannel == null)
                return;
            onLeftChannel(this, new OnLeftChannelArgs
            {
                BotUsername = TwitchUsername,
                Channel = ircMessage.Channel
            });
        }
        else
        {
            var onUserLeft = OnUserLeft;
            if (onUserLeft == null)
                return;
            onUserLeft(this, new OnUserLeftArgs
            {
                Channel = ircMessage.Channel,
                Username = ircMessage.User
            });
        }
    }

    private void HandleClearChat(IrcMessage ircMessage)
    {
        if (string.IsNullOrWhiteSpace(ircMessage.Message))
        {
            var onChatCleared = OnChatCleared;
            if (onChatCleared == null)
                return;
            onChatCleared(this, new OnChatClearedArgs
            {
                Channel = ircMessage.Channel
            });
        }
        else if (ircMessage.Tags.TryGetValue("ban-duration", out var _))
        {
            var userTimeout = new UserTimeout(ircMessage);
            var onUserTimedout = OnUserTimedout;
            if (onUserTimedout == null)
                return;
            onUserTimedout(this, new OnUserTimedoutArgs
            {
                UserTimeout = userTimeout
            });
        }
        else
        {
            var userBan = new UserBan(ircMessage);
            var onUserBanned = OnUserBanned;
            if (onUserBanned == null)
                return;
            onUserBanned(this, new OnUserBannedArgs
            {
                UserBan = userBan
            });
        }
    }

    private void HandleClearMsg(IrcMessage ircMessage)
    {
        var onMessageCleared = OnMessageCleared;
        if (onMessageCleared == null)
            return;
        onMessageCleared(this, new OnMessageClearedArgs
        {
            Channel = ircMessage.Channel,
            Message = ircMessage.Message,
            TargetMessageId = ircMessage.ToString().Split('=')[3].Split(';')[0],
            TmiSentTs = ircMessage.ToString().Split('=')[4].Split(' ')[0]
        });
    }

    private void HandleUserState(IrcMessage ircMessage)
    {
        var state = new UserState(ircMessage);
        if (!_hasSeenJoinedChannels.Contains(state.Channel.ToLowerInvariant()))
        {
            _hasSeenJoinedChannels.Add(state.Channel.ToLowerInvariant());
            var userStateChanged = OnUserStateChanged;
            if (userStateChanged == null)
                return;
            userStateChanged(this, new OnUserStateChangedArgs
            {
                UserState = state
            });
        }
        else
        {
            var onMessageSent = OnMessageSent;
            if (onMessageSent == null)
                return;
            onMessageSent(this, new OnMessageSentArgs
            {
                SentMessage = new SentMessage(state, _lastMessageSent)
            });
        }
    }

    private void Handle004()
    {
        var onConnected = OnConnected;
        if (onConnected == null)
            return;
        onConnected(this, new OnConnectedArgs
        {
            BotUsername = TwitchUsername
        });
    }

    private void Handle353(IrcMessage ircMessage)
    {
        var existingUsersDetected = OnExistingUsersDetected;
        if (existingUsersDetected == null)
            return;
        existingUsersDetected(this, new OnExistingUsersDetectedArgs
        {
            Channel = ircMessage.Channel,
            Users = ircMessage.Message.Split(' ').ToList()
        });
    }

    private void Handle366()
    {
        _currentlyJoiningChannels = false;
        QueueingJoinCheck();
    }

    private void HandleWhisper(IrcMessage ircMessage)
    {
        var whisperMessage = new WhisperMessage(ircMessage, TwitchUsername);
        PreviousWhisper = whisperMessage;
        var onWhisperReceived = OnWhisperReceived;
        if (onWhisperReceived != null)
            onWhisperReceived(this, new OnWhisperReceivedArgs
            {
                WhisperMessage = whisperMessage
            });
        if (_whisperCommandIdentifiers != null && _whisperCommandIdentifiers.Count != 0 &&
            !string.IsNullOrEmpty(whisperMessage.Message) &&
            _whisperCommandIdentifiers.Contains(whisperMessage.Message[0]))
        {
            var whisperCommand = new WhisperCommand(whisperMessage);
            var whisperCommandReceived = OnWhisperCommandReceived;
            if (whisperCommandReceived == null)
                return;
            whisperCommandReceived(this, new OnWhisperCommandReceivedArgs
            {
                Command = whisperCommand
            });
        }
        else
        {
            var onUnaccountedFor = OnUnaccountedFor;
            if (onUnaccountedFor != null)
                onUnaccountedFor(this, new OnUnaccountedForArgs
                {
                    BotUsername = TwitchUsername,
                    Channel = ircMessage.Channel,
                    Location = "WhispergHandling",
                    RawIRC = ircMessage.ToString()
                });
            UnaccountedFor(ircMessage.ToString());
        }
    }

    private void HandleRoomState(IrcMessage ircMessage)
    {
        if (ircMessage.Tags.Count > 2)
        {
            _awaitingJoins.Remove(
                _awaitingJoins.FirstOrDefault(
                    (Func<KeyValuePair<string, DateTime>, bool>)(x => x.Key == ircMessage.Channel)));
            var onJoinedChannel = OnJoinedChannel;
            if (onJoinedChannel != null)
                onJoinedChannel(this, new OnJoinedChannelArgs
                {
                    BotUsername = TwitchUsername,
                    Channel = ircMessage.Channel
                });
        }

        var channelStateChanged = OnChannelStateChanged;
        if (channelStateChanged == null)
            return;
        channelStateChanged(this, new OnChannelStateChangedArgs
        {
            ChannelState = new ChannelState(ircMessage),
            Channel = ircMessage.Channel
        });
    }

    private void HandleUserNotice(IrcMessage ircMessage)
    {
        if (!ircMessage.Tags.TryGetValue("msg-id", out var str))
        {
            var onUnaccountedFor = OnUnaccountedFor;
            if (onUnaccountedFor != null)
                onUnaccountedFor(this, new OnUnaccountedForArgs
                {
                    BotUsername = TwitchUsername,
                    Channel = ircMessage.Channel,
                    Location = "UserNoticeHandling",
                    RawIRC = ircMessage.ToString()
                });
            UnaccountedFor(ircMessage.ToString());
        }
        else
        {
            switch (str)
            {
                case "announcement":
                    var announcement = new Announcement(ircMessage);
                    var onAnnouncement = OnAnnouncement;
                    if (onAnnouncement == null)
                        break;
                    onAnnouncement(this, new OnAnnouncementArgs
                    {
                        Announcement = announcement,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "giftpaidupgrade":
                    var giftedSubscription1 = new ContinuedGiftedSubscription(ircMessage);
                    var giftedSubscription2 =
                        OnContinuedGiftedSubscription;
                    if (giftedSubscription2 == null)
                        break;
                    giftedSubscription2(this, new OnContinuedGiftedSubscriptionArgs
                    {
                        ContinuedGiftedSubscription = giftedSubscription1,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "primepaidupgrade":
                    var primePaidSubscriber1 = new PrimePaidSubscriber(ircMessage);
                    var primePaidSubscriber2 = OnPrimePaidSubscriber;
                    if (primePaidSubscriber2 == null)
                        break;
                    primePaidSubscriber2(this, new OnPrimePaidSubscriberArgs
                    {
                        PrimePaidSubscriber = primePaidSubscriber1,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "raid":
                    var raidNotification1 = new RaidNotification(ircMessage);
                    var raidNotification2 = OnRaidNotification;
                    if (raidNotification2 == null)
                        break;
                    raidNotification2(this, new OnRaidNotificationArgs
                    {
                        Channel = ircMessage.Channel,
                        RaidNotification = raidNotification1
                    });
                    break;
                case "resub":
                    var reSubscriber = new ReSubscriber(ircMessage);
                    var onReSubscriber = OnReSubscriber;
                    if (onReSubscriber == null)
                        break;
                    onReSubscriber(this, new OnReSubscriberArgs
                    {
                        ReSubscriber = reSubscriber,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "sub":
                    var subscriber = new Subscriber(ircMessage);
                    var onNewSubscriber = OnNewSubscriber;
                    if (onNewSubscriber == null)
                        break;
                    onNewSubscriber(this, new OnNewSubscriberArgs
                    {
                        Subscriber = subscriber,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "subgift":
                    var giftedSubscription3 = new GiftedSubscription(ircMessage);
                    var giftedSubscription4 = OnGiftedSubscription;
                    if (giftedSubscription4 == null)
                        break;
                    giftedSubscription4(this, new OnGiftedSubscriptionArgs
                    {
                        GiftedSubscription = giftedSubscription3,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "submysterygift":
                    var communitySubscription1 = new CommunitySubscription(ircMessage);
                    var communitySubscription2 = OnCommunitySubscription;
                    if (communitySubscription2 == null)
                        break;
                    communitySubscription2(this, new OnCommunitySubscriptionArgs
                    {
                        GiftedSubscription = communitySubscription1,
                        Channel = ircMessage.Channel
                    });
                    break;
                default:
                    var onUnaccountedFor1 = OnUnaccountedFor;
                    if (onUnaccountedFor1 != null)
                        onUnaccountedFor1(this, new OnUnaccountedForArgs
                        {
                            BotUsername = TwitchUsername,
                            Channel = ircMessage.Channel,
                            Location = "UserNoticeHandling",
                            RawIRC = ircMessage.ToString()
                        });
                    UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }

    private void HandleMode(IrcMessage ircMessage)
    {
        if (ircMessage.Message.StartsWith("+o"))
        {
            var onModeratorJoined = OnModeratorJoined;
            if (onModeratorJoined == null)
                return;
            onModeratorJoined(this, new OnModeratorJoinedArgs
            {
                Channel = ircMessage.Channel,
                Username = ircMessage.Message.Split(' ')[1]
            });
        }
        else
        {
            if (!ircMessage.Message.StartsWith("-o"))
                return;
            var onModeratorLeft = OnModeratorLeft;
            if (onModeratorLeft == null)
                return;
            onModeratorLeft(this, new OnModeratorLeftArgs
            {
                Channel = ircMessage.Channel,
                Username = ircMessage.Message.Split(' ')[1]
            });
        }
    }

    private void HandleCap(IrcMessage ircMessage)
    {
        logger.LogInfo(ircMessage.Message);
    }

    private void UnaccountedFor(string ircString)
    {
        logger.LogInfo("Unaccounted for: " + ircString + " (please create a TwitchLib GitHub issue :P)");
    }

    private static void HandleNotInitialized()
    {
        throw new ClientNotInitializedException(
            "The twitch client has not been initialized and cannot be used. Please call Initialize();");
    }

    private static void HandleNotConnected()
    {
        throw new ClientNotConnectedException(
            "In order to perform this action, the client must be connected to Twitch. To confirm connection, try performing this action in or after the OnConnected event has been fired.");
    }
}