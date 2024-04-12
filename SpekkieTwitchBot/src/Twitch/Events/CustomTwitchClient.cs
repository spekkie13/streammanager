using System.Reflection;
using System.Timers;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Twitch.Client;
using SpekkieTwitchBot.Twitch.Events.Interfaces;
using SpekkieTwitchBot.Twitch.General;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Enums.Internal;
using TwitchLib.Client.Events;
using TwitchLib.Client.Exceptions;
using TwitchLib.Client.Internal;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;
using TwitchLib.Communication.Events;
using Timer = System.Timers.Timer;

namespace SpekkieTwitchBot.Twitch.Events
{
    public class CustomTwitchClient : ICustomTwitchClient
    {
        private CustomClient _client;
        private readonly Logger _GeneralLogger;
        private MessageEmoteCollection _channelEmotes = new ();
        private readonly ICollection<char> _chatCommandIdentifiers = new HashSet<char>();
        private readonly ICollection<char> _whisperCommandIdentifiers = new HashSet<char>();
        private readonly Queue<JoinedChannel> _joinChannelQueue = new ();
        private readonly ClientProtocol _protocol;
        private bool _currentlyJoiningChannels;
        private Timer _joinTimer;
        private List<KeyValuePair<string, DateTime>> _awaitingJoins;
        private readonly IrcParser _ircParser;
        private readonly JoinedChannelManager _joinedChannelManager;
        private readonly List<string> _hasSeenJoinedChannels = new ();
        private string? _lastMessageSent;

        public Version Version => Assembly.GetEntryAssembly().GetName().Version;
        
        public IReadOnlyList<JoinedChannel> JoinedChannels => _joinedChannelManager.GetJoinedChannels();

        public string TwitchUsername { get; private set; }

        public WhisperMessage? PreviousWhisper { get; private set; }

        public bool IsConnected => _client is { IsConnected: true };

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
        public event EventHandler<OnPrimePaidSubscriberArgs> OnPrimePaidSubscriber;
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
        public event EventHandler<OnContinuedGiftedSubscriptionArgs> OnContinuedGiftedSubscription;
        public event EventHandler<OnMessageThrottledEventArgs> OnMessageThrottled;
        public event EventHandler<OnWhisperThrottledEventArgs> OnWhisperThrottled;
        public event EventHandler<OnErrorEventArgs> OnError;
        public event EventHandler<OnReconnectedEventArgs> OnReconnected;
        public event EventHandler<OnRequiresVerifiedEmailArgs> OnRequiresVerifiedEmail;
        public event EventHandler<OnRequiresVerifiedPhoneNumberArgs> OnRequiresVerifiedPhoneNumber;
        public event EventHandler<OnRateLimitArgs> OnRateLimit;
        public event EventHandler<OnDuplicateArgs> OnDuplicate;
        public event EventHandler<OnBannedEmailAliasArgs> OnBannedEmailAlias;
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
        public event EventHandler<OnUserIntroArgs> OnUserIntro;
        public event EventHandler<OnUnaccountedForArgs> OnUnaccountedFor;

        public CustomTwitchClient(
            Logger generalLogger,
            CustomClient client,
            ClientProtocol protocol = ClientProtocol.WebSocket
            )
        {
            _GeneralLogger = generalLogger;
            _client = client;
            _protocol = protocol;
            _joinedChannelManager = new JoinedChannelManager();
            _ircParser = new IrcParser();
        }

        public void Initialize(
            ConnectionCredentials credentials,
            string channel = "",
            char chatCommandIdentifier = '!',
            char whisperCommandIdentifier = '!',
            bool autoReListenOnExceptions = true)
        {
            if (channel[0] == '#')
                channel = channel[1..];
            List<string> channels = new List<string> { channel };
            int chatCommandIdentifier1 = chatCommandIdentifier;
            int whisperCommandIdentifier1 = whisperCommandIdentifier;
            int num = autoReListenOnExceptions ? 1 : 0;
            InitializeHelper(credentials, channels, (char)chatCommandIdentifier1, (char)whisperCommandIdentifier1,
                num != 0);
        }

        public void Initialize(ConnectionCredentials credentials,
            List<string> channels,
            char chatCommandIdentifier = '!',
            char whisperCommandIdentifier = '!',
            bool autoReListenOnExceptions = true)
        {
            channels = channels.Select((Func<string, string>)(x => x[0] != '#' ? x : x[1..])).ToList();
            InitializeHelper(credentials, channels, chatCommandIdentifier, whisperCommandIdentifier,
                autoReListenOnExceptions);
        }

        private void InitializeHelper(
            ConnectionCredentials credentials,
            List<string> channels,
            char chatCommandIdentifier = '!',
            char whisperCommandIdentifier = '!',
            bool autoReListenOnExceptions = true)
        {
            _GeneralLogger.LogInfo($"CustomTwitchClient initialized, assembly version: {Assembly.GetExecutingAssembly().GetName().Version}");
            ConnectionCredentials = credentials;
            TwitchUsername = ConnectionCredentials.TwitchUsername;
            if (chatCommandIdentifier != char.MinValue)
                _chatCommandIdentifiers.Add(chatCommandIdentifier);
            if (whisperCommandIdentifier != char.MinValue)
                _whisperCommandIdentifiers.Add(whisperCommandIdentifier);
            AutoReListenOnException = autoReListenOnExceptions;
            if (channels is { Count: > 0 })
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    if (!string.IsNullOrEmpty(channels[i]))
                    {
                        if (JoinedChannels.FirstOrDefault(
                                (Func<JoinedChannel, bool>)(x => x.Channel.ToLower() == channels[i])) != null)
                            return;
                        _joinChannelQueue.Enqueue(new JoinedChannel(channels[i]));
                    }
                }
            }

            InitializeClient();
        }

        private void InitializeClient()
        {
            _client = new CustomClient();
            
            _client.OnConnected += _client_OnConnected;
            _client.OnMessage += _client_OnMessage;
            _client.OnDisconnected += _client_OnDisconnected;
            _client.OnFatality += _client_OnFatality;
            _client.OnMessageThrottled += _client_OnMessageThrottled;
            _client.OnWhisperThrottled += _client_OnWhisperThrottled;
            _client.OnReconnected += _client_OnReconnected;
            
        }

        internal void RaiseEvent(string eventName, object? args = null)
        {
            foreach (Delegate invocation in (GetType()
                         .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                         .GetValue(this) as MulticastDelegate).GetInvocationList())
            {
                MethodInfo method = invocation.Method;
                object? target = invocation.Target;
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

        public void SendRaw(string message)
        {
            _GeneralLogger.LogInfo("Writing: " + message);
            _client.Send(message);
            OnSendReceiveData(this, new OnSendReceiveDataArgs
            {
                Direction = SendReceiveDirection.Sent,
                Data = message
            });
        }

        private void SendTwitchMessage(
            JoinedChannel? channel,
            string? message,
            string? replyToId = "",
            bool dryRun = false)
        {
            if (((channel == null ? 1 : (message == null ? 1 : 0)) | (dryRun ? 1 : 0)) != 0)
                return;
            if (message?.Length > 500)
            {
                _GeneralLogger.LogError("Message length has exceeded the maximum character count. (500)");
            }
            else
            {
                OutboundChatMessage outboundChatMessage = new OutboundChatMessage
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

        public void SendMessage(JoinedChannel? channel, string? message, bool dryRun = false)
        {
            SendTwitchMessage(channel, message, dryRun: dryRun);
        }

        public void SendMessage(string? channel, string? message, bool dryRun = false)
        {
            SendMessage(GetJoinedChannel(channel), message, dryRun);
        }

        public void SendReply(JoinedChannel? channel, string? replyToId, string? message, bool dryRun = false)
        {
            SendTwitchMessage(channel, message, replyToId, dryRun);
        }

        public void SendReply(string? channel, string? replyToId, string? message, bool dryRun = false)
        {
            SendReply(GetJoinedChannel(channel), replyToId, message, dryRun);
        }

        public void SendWhisper(string receiver, string message, bool dryRun = false)
        {
            if (dryRun)
                return;
            _client.SendWhisper(new OutboundWhisperMessage
            {
                Receiver = receiver,
                Username = ConnectionCredentials.TwitchUsername,
                Message = message
            }.ToString());
            OnWhisperSent(this, new OnWhisperSentArgs
            {
                Receiver = receiver,
                Message = message
            });
        }

        public bool Connect()
        {
            _GeneralLogger.LogInfo("Connecting to: " + ConnectionCredentials.TwitchWebsocketURI);
            _joinedChannelManager.Clear();
            if (!_client.Open())
                return false;
            _GeneralLogger.LogInfo("Should be connected!");
            return true;
        }

        public void Disconnect()
        {
            _GeneralLogger.LogInfo("Disconnect Twitch Chat Client...");
            _client.Close();
            _joinedChannelManager.Clear();
            PreviousWhisper = null;
        }

        public void Reconnect()
        {
            _GeneralLogger.LogWarning("Reconnecting to Twitch");
            _client.Reconnect();
        }

        public void AddChatCommandIdentifier(char identifier)
        {
            _chatCommandIdentifiers.Add(identifier);
        }

        public void RemoveChatCommandIdentifier(char identifier)
        {
            _chatCommandIdentifiers.Remove(identifier);
        }

        public void AddWhisperCommandIdentifier(char identifier)
        {
            _whisperCommandIdentifiers.Add(identifier);
        }

        public void RemoveWhisperCommandIdentifier(char identifier)
        {
            _whisperCommandIdentifiers.Remove(identifier);
        }

        public void SetConnectionCredentials(ConnectionCredentials credentials)
        {
            if (IsConnected)
                throw new IllegalAssignmentException(
                    "While the client is connected, you are unable to change the connection credentials. Please disconnect first and then change them.");
            ConnectionCredentials = credentials;
        }

        public void JoinChannel(string channel, bool overrideCheck = false)
        {
            if (!IsConnected)
                HandleNotConnected();
            if (JoinedChannels.FirstOrDefault((Func<JoinedChannel, bool>)(x =>
                    x.Channel.ToLower() == channel && !overrideCheck)) != null)
                return;
            if (channel[0] == '#')
                channel = channel[1..];
            _joinChannelQueue.Enqueue(new JoinedChannel(channel));
            if (_currentlyJoiningChannels)
                return;
            QueueingJoinCheck();
        }

        public JoinedChannel? GetJoinedChannel(string? channel)
        {
            if (JoinedChannels.Count == 0)
                throw new BadStateException("Must be connected to at least one channel.");
            if (channel?[0] == '#')
                channel = channel[1..];
            return _joinedChannelManager.GetJoinedChannel(channel);
        }

        public void LeaveChannel(string channel)
        {
            channel = channel.ToLower();
            if (channel[0] == '#')
                channel = channel[1..];
            _GeneralLogger.LogInfo($"Leaving channel: {channel}");
            if (_joinedChannelManager.GetJoinedChannel(channel) == null)
                return;
            _client.Send(Rfc2812.Part("#" + channel));
        }

        public void LeaveChannel(JoinedChannel channel)
        {
            LeaveChannel(channel.Channel);
        }

        public void OnReadLineTest(string rawIrc)
        {
            HandleIrcMessage(_ircParser.ParseIrcMessage(rawIrc));
        }

        private void _client_OnWhisperThrottled(object? sender, OnWhisperThrottledEventArgs e)
        {
            OnWhisperThrottled(sender, e);
        }

        private void _client_OnMessageThrottled(object? sender, OnMessageThrottledEventArgs e)
        {
            OnMessageThrottled(sender, e);
        }

        private void _client_OnFatality(object? sender, OnFatalErrorEventArgs e)
        {
            OnConnectionError(this, new OnConnectionErrorArgs
            {
                BotUsername = TwitchUsername,
                Error = new ErrorEvent { Message = e.Reason }
            });
        }

        private void _client_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            OnDisconnected(sender, e);
        }

        private void _client_OnReconnected(object? sender, OnReconnectedEventArgs e)
        {
            foreach (JoinedChannel joinedChannel in
                     (IEnumerable<JoinedChannel>)_joinedChannelManager.GetJoinedChannels())
            {
                if (!string.Equals(joinedChannel.Channel, TwitchUsername, StringComparison.CurrentCultureIgnoreCase))
                    _joinChannelQueue.Enqueue(joinedChannel);
            }

            _joinedChannelManager.Clear();
            OnReconnected(sender, e);
        }

        private void _client_OnMessage(object? sender, OnMessageEventArgs e)
        {
            string[] separator = { "\r\n" };
            foreach (string raw in e.Message.Split(separator, StringSplitOptions.None))
            {
                if (raw.Length <= 1) continue;
                _GeneralLogger.LogInfo("Received: " + raw);
                OnSendReceiveData(this, new OnSendReceiveDataArgs
                {
                    Direction = SendReceiveDirection.Received,
                    Data = raw
                });
                HandleIrcMessage(_ircParser.ParseIrcMessage(raw));
            }
        }

        private void _client_OnConnected(object? sender, object e)
        {
            _client.Send(Rfc2812.Pass(ConnectionCredentials.TwitchOAuth));
            _client.Send(Rfc2812.Nick(ConnectionCredentials.TwitchUsername));
            _client.Send(Rfc2812.User(ConnectionCredentials.TwitchUsername, 0, ConnectionCredentials.TwitchUsername));
            if (ConnectionCredentials.Capabilities.Membership)
                _client.Send("CAP REQ twitch.tv/membership");
            if (ConnectionCredentials.Capabilities.Commands)
                _client.Send("CAP REQ twitch.tv/commands");
            if (ConnectionCredentials.Capabilities.Tags)
                _client.Send("CAP REQ twitch.tv/tags");
            if (_joinChannelQueue is not { Count: > 0 })
                return;
            QueueingJoinCheck();
        }

        private void QueueingJoinCheck()
        {
            if (_joinChannelQueue.Count > 0)
            {
                _currentlyJoiningChannels = true;
                JoinedChannel joinedChannel = _joinChannelQueue.Dequeue();
                _GeneralLogger.LogInfo($"Joining channel: {joinedChannel.Channel}");
                _client.Send(Rfc2812.Join("#" + joinedChannel.Channel.ToLower()));
                _joinedChannelManager.AddJoinedChannel(new JoinedChannel(joinedChannel.Channel));
                StartJoinedChannelTimer(joinedChannel.Channel);
            }
            else
                _GeneralLogger.LogInfo("Finished channel joining queue.");
        }

        private void StartJoinedChannelTimer(string channel)
        {
            
            _joinTimer = new Timer(1000.0);
            _joinTimer.Elapsed += JoinChannelTimeout;
            _awaitingJoins = new List<KeyValuePair<string, DateTime>> { new (channel.ToLower(), DateTime.Now) };

            if (_joinTimer.Enabled)
                return;
            _joinTimer.Start();
        }

        private void JoinChannelTimeout(object sender, ElapsedEventArgs e)
        {
            if (_awaitingJoins.Any())
            {
                List<KeyValuePair<string, DateTime>> list = _awaitingJoins.Where((Func<KeyValuePair<string, DateTime>, bool>) (x => (DateTime.Now - x.Value).TotalSeconds > 5.0)).ToList();
                if (!list.Any())
                    return;
                _awaitingJoins.RemoveAll((Predicate<KeyValuePair<string, DateTime>>) (x => (DateTime.Now - x.Value).TotalSeconds > 5.0));
                foreach (KeyValuePair<string, DateTime> keyValuePair in list)
                {
                    _joinedChannelManager.RemoveJoinedChannel(keyValuePair.Key.ToLowerInvariant());
                    EventHandler<OnFailureToReceiveJoinConfirmationArgs> joinConfirmation = OnFailureToReceiveJoinConfirmation;
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
                OnIncorrectLogin(this, new OnIncorrectLoginArgs
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
                        OnUnaccountedFor(this, new OnUnaccountedForArgs
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
            ChatMessage chatMessage = new ChatMessage(TwitchUsername, ircMessage, ref _channelEmotes, WillReplaceEmotes);
            foreach (JoinedChannel joinedChannel in JoinedChannels.Where((Func<JoinedChannel, bool>)(x =>
                         string.Equals(x.Channel, ircMessage.Channel, StringComparison.InvariantCultureIgnoreCase))))
                joinedChannel.HandleMessage(chatMessage);
            OnMessageReceived(this, new OnMessageReceivedArgs
            {
                ChatMessage = chatMessage
            });
            if (ircMessage.Tags.TryGetValue("msg-id", out var str) && str == "user-intro")
            {
                OnUserIntro(this, new OnUserIntroArgs
                {
                    ChatMessage = chatMessage
                });
            }

            if (_chatCommandIdentifiers.Count == 0 ||
                string.IsNullOrEmpty(chatMessage.Message) || !_chatCommandIdentifiers.Contains(chatMessage.Message[0]))
                return;
            ChatCommand chatCommand = new ChatCommand(chatMessage);
            OnChatCommandReceived(this, new OnChatCommandReceivedArgs
            {
                Command = chatCommand
            });
        }

        private void HandleNotice(IrcMessage ircMessage)
        {
            if (ircMessage.Message.Contains("Improperly formatted auth"))
            {
                OnIncorrectLogin(this, new OnIncorrectLoginArgs
                {
                    Exception = new ErrorLoggingInException(ircMessage.ToString(), TwitchUsername)
                });
            }
            else
            {
                if (!ircMessage.Tags.TryGetValue("msg-id", out var str))
                {
                    OnUnaccountedFor(this, new OnUnaccountedForArgs
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
                        OnChatColorChanged(this, new OnChatColorChangedArgs
                        {
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "msg_banned":
                        OnBanned(this, new OnBannedArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_banned_email_alias":
                        OnBannedEmailAlias(this, new OnBannedEmailAliasArgs
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
                        OnFailureToReceiveJoinConfirmation(this, new OnFailureToReceiveJoinConfirmationArgs
                        {
                            Exception = new FailureToReceiveJoinConfirmationException(ircMessage.Channel,
                                ircMessage.Message)
                        });
                        break;
                    case "msg_duplicate":
                        OnDuplicate(this, new OnDuplicateArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_emoteonly":
                        OnEmoteOnly(this, new OnEmoteOnlyArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_followersonly":
                        OnFollowersOnly(this, new OnFollowersOnlyArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_r9k":
                        OnR9KMode(this, new OnR9kModeArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_ratelimit":
                        OnRateLimit(this, new OnRateLimitArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_requires_verified_phone_number":
                        OnRequiresVerifiedPhoneNumber(this, new OnRequiresVerifiedPhoneNumberArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_slowmode":
                        OnSlowMode(this, new OnSlowModeArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_subsonly":
                        OnSubsOnly(this, new OnSubsOnlyArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_suspended":
                        OnSuspended(this, new OnSuspendedArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "msg_verified_email":
                        OnRequiresVerifiedEmail(this, new OnRequiresVerifiedEmailArgs
                        {
                            Channel = ircMessage.Channel,
                            Message = ircMessage.Message
                        });
                        break;
                    case "no_mods":
                        OnModeratorsReceived(this, new OnModeratorsReceivedArgs
                        {
                            Channel = ircMessage.Channel,
                            Moderators = new List<string>()
                        });
                        break;
                    case "no_permission":
                        OnNoPermissionError(this, EventArgs.Empty);
                        break;
                    case "no_vips":
                        OnVIPsReceived(this, new OnVIPsReceivedArgs
                        {
                            Channel = ircMessage.Channel,
                            VIPs = new List<string>()
                        });
                        break;
                    case "raid_error_self":
                        OnSelfRaidError(this, EventArgs.Empty);
                        break;
                    case "raid_notice_mature":
                        OnRaidedChannelIsMatureAudience(this, EventArgs.Empty);
                        break;
                    case "room_mods":
                        OnModeratorsReceived(this, new OnModeratorsReceivedArgs
                        {
                            Channel = ircMessage.Channel,
                            Moderators = ircMessage.Message.Replace(" ", "").Split(':')[1].Split(',').ToList()
                        });
                        break;
                    case "vips_success":
                        OnVIPsReceived(this, new OnVIPsReceivedArgs
                        {
                            Channel = ircMessage.Channel,
                            VIPs = ircMessage.Message.Replace(" ", "").Replace(".", "").Split(':')[1].Split(',')
                                .ToList()
                        });
                        break;
                    default:
                        OnUnaccountedFor(this, new OnUnaccountedForArgs
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
            OnUserJoined(this, new OnUserJoinedArgs
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
                OnLeftChannel(this, new OnLeftChannelArgs
                {
                    BotUsername = TwitchUsername,
                    Channel = ircMessage.Channel
                });
            }
            else
            {
                OnUserLeft(this, new OnUserLeftArgs
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
                OnChatCleared(this, new OnChatClearedArgs
                {
                    Channel = ircMessage.Channel
                });
            }
            else if (ircMessage.Tags.TryGetValue("ban-duration", out string _))
            {
                UserTimeout userTimeout = new UserTimeout(ircMessage);
                OnUserTimedout(this, new OnUserTimedoutArgs
                {
                    UserTimeout = userTimeout
                });
            }
            else
            {
                UserBan userBan = new UserBan(ircMessage);
                OnUserBanned(this, new OnUserBannedArgs
                {
                    UserBan = userBan
                });
            }
        }

        private void HandleClearMsg(IrcMessage ircMessage)
        {
            OnMessageCleared(this, new OnMessageClearedArgs
            {
                Channel = ircMessage.Channel,
                Message = ircMessage.Message,
                TargetMessageId = ircMessage.ToString().Split('=')[3].Split(';')[0],
                TmiSentTs = ircMessage.ToString().Split('=')[4].Split(' ')[0]
            });
        }

        private void HandleUserState(IrcMessage ircMessage)
        {
            UserState state = new UserState(ircMessage);
            if (!_hasSeenJoinedChannels.Contains(state.Channel.ToLowerInvariant()))
            {
                _hasSeenJoinedChannels.Add(state.Channel.ToLowerInvariant());
                OnUserStateChanged(this, new OnUserStateChangedArgs
                {
                    UserState = state
                });
            }
            else
            {
                OnMessageSent(this, new OnMessageSentArgs
                {
                    SentMessage = new SentMessage(state, _lastMessageSent)
                });
            }
        }

        private void Handle004()
        {
            OnConnected(this, new OnConnectedArgs
            {
                BotUsername = TwitchUsername
            });
        }

        private void Handle353(IrcMessage ircMessage)
        {
            OnExistingUsersDetected(this, new OnExistingUsersDetectedArgs
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
            WhisperMessage whisperMessage = new WhisperMessage(ircMessage, TwitchUsername);
            PreviousWhisper = whisperMessage;
            OnWhisperReceived(this, new OnWhisperReceivedArgs
            {
                WhisperMessage = whisperMessage
            });
            if (_whisperCommandIdentifiers.Count != 0 &&
                !string.IsNullOrEmpty(whisperMessage.Message) &&
                _whisperCommandIdentifiers.Contains(whisperMessage.Message[0]))
            {
                WhisperCommand whisperCommand = new WhisperCommand(whisperMessage);
                OnWhisperCommandReceived(this, new OnWhisperCommandReceivedArgs
                {
                    Command = whisperCommand
                });
            }
            else
            {
                OnUnaccountedFor(this, new OnUnaccountedForArgs
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
                OnJoinedChannel(this, new OnJoinedChannelArgs
                {
                    BotUsername = TwitchUsername,
                    Channel = ircMessage.Channel
                });
            }

            OnChannelStateChanged(this, new OnChannelStateChangedArgs
            {
                ChannelState = new ChannelState(ircMessage),
                Channel = ircMessage.Channel
            });
        }

        private void HandleUserNotice(IrcMessage ircMessage)
        {
            if (!ircMessage.Tags.TryGetValue("msg-id", out var str))
            {
                OnUnaccountedFor(this, new OnUnaccountedForArgs
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
                        Announcement announcement = new Announcement(ircMessage);
                        OnAnnouncement(this, new OnAnnouncementArgs
                        {
                            Announcement = announcement,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "giftpaidupgrade":
                        ContinuedGiftedSubscription giftedSubscription1 = new ContinuedGiftedSubscription(ircMessage);
                        OnContinuedGiftedSubscription(this, new OnContinuedGiftedSubscriptionArgs
                        {
                            ContinuedGiftedSubscription = giftedSubscription1,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "primepaidupgrade":
                        PrimePaidSubscriber primePaidSubscriber1 = new PrimePaidSubscriber(ircMessage);
                        OnPrimePaidSubscriber(this, new OnPrimePaidSubscriberArgs
                        {
                            PrimePaidSubscriber = primePaidSubscriber1,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "raid":
                        RaidNotification raidNotification1 = new RaidNotification(ircMessage);
                        OnRaidNotification(this, new OnRaidNotificationArgs
                        {
                            Channel = ircMessage.Channel,
                            RaidNotification = raidNotification1
                        });
                        break;
                    case "resub":
                        ReSubscriber reSubscriber = new ReSubscriber(ircMessage);
                        OnReSubscriber(this, new OnReSubscriberArgs
                        {
                            ReSubscriber = reSubscriber,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "sub":
                        Subscriber subscriber = new Subscriber(ircMessage);
                        OnNewSubscriber(this, new OnNewSubscriberArgs
                        {
                            Subscriber = subscriber,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "subgift":
                        GiftedSubscription giftedSubscription3 = new GiftedSubscription(ircMessage);
                        OnGiftedSubscription(this, new OnGiftedSubscriptionArgs
                        {
                            GiftedSubscription = giftedSubscription3,
                            Channel = ircMessage.Channel
                        });
                        break;
                    case "submysterygift":
                        CommunitySubscription communitySubscription1 = new CommunitySubscription(ircMessage);
                        OnCommunitySubscription(this, new OnCommunitySubscriptionArgs
                        {
                            GiftedSubscription = communitySubscription1,
                            Channel = ircMessage.Channel
                        });
                        break;
                    default:
                        OnUnaccountedFor(this, new OnUnaccountedForArgs
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
                OnModeratorJoined(this, new OnModeratorJoinedArgs
                {
                    Channel = ircMessage.Channel,
                    Username = ircMessage.Message.Split(' ')[1]
                });
            }
            else
            {
                if (!ircMessage.Message.StartsWith("-o"))
                    return;
                OnModeratorLeft(this, new OnModeratorLeftArgs
                {
                    Channel = ircMessage.Channel,
                    Username = ircMessage.Message.Split(' ')[1]
                });
            }
        }

        private void HandleCap(IrcMessage ircMessage)
        {
            _GeneralLogger.LogInfo($"{ircMessage.Message}");
        }

        private void UnaccountedFor(string ircString)
        {
            _GeneralLogger.LogWarning($"Unaccounted for: {ircString} (please create a TwitchLib GitHub issue :P)");
        }

        public void SendQueuedItem(string message)
        {
            _client.Send(message);
        }

        private static void HandleNotConnected()
        {
            throw new ClientNotConnectedException(
                "In order to perform this action, the client must be connected to Twitch. To confirm connection, try performing this action in or after the OnConnected event has been fired.");
        }
    }
}