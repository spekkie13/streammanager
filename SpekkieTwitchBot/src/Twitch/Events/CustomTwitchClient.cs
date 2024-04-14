#nullable disable
using System.Reflection;
using System.Timers;
using Microsoft.Extensions.Logging;
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
using SpekkieTwitchBot.Twitch.General;
namespace SpekkieTwitchBot.Twitch.Events;

public class CustomTwitchClient : ITwitchClient
{
    private IClient _client;
    private MessageEmoteCollection _channelEmotes = new MessageEmoteCollection();
    private readonly ICollection<char> _chatCommandIdentifiers = (ICollection<char>)new HashSet<char>();
    private readonly ICollection<char> _whisperCommandIdentifiers = (ICollection<char>)new HashSet<char>();
    private readonly Queue<JoinedChannel> _joinChannelQueue = new Queue<JoinedChannel>();
    private readonly ILogger<CustomTwitchClient> _logger;
    private readonly ClientProtocol _protocol;
    private bool _currentlyJoiningChannels;
    private System.Timers.Timer _joinTimer;
    private List<KeyValuePair<string, DateTime>> _awaitingJoins;
    private readonly IrcParser _ircParser;
    private readonly JoinedChannelManager _joinedChannelManager;
    private readonly List<string> _hasSeenJoinedChannels = new List<string>();
    private string _lastMessageSent;
    public Version Version => Assembly.GetEntryAssembly().GetName().Version;
    public bool IsInitialized => this._client != null;
    public IReadOnlyList<JoinedChannel> JoinedChannels
    {
        get => this._joinedChannelManager.GetJoinedChannels();
    }
    public string TwitchUsername { get; private set; }
    public WhisperMessage PreviousWhisper { get; private set; }
    public bool IsConnected
    {
        get => this.IsInitialized && this._client != null && this._client.IsConnected;
    }
    public MessageEmoteCollection ChannelEmotes => this._channelEmotes;
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
    public event EventHandler<OnR9kModeArgs> OnR9kMode;
    public event EventHandler<OnUserIntroArgs> OnUserIntro;
    public event EventHandler<OnUnaccountedForArgs> OnUnaccountedFor;

    public CustomTwitchClient(IClient client = null, ClientProtocol protocol = ClientProtocol.WebSocket,
        ILogger<CustomTwitchClient> logger = null)
    {
        this._logger = logger;
        this._client = client;
        this._protocol = protocol;
        this._joinedChannelManager = new JoinedChannelManager();
        this._ircParser = new IrcParser();
    }

    public void Initialize(
        ConnectionCredentials credentials,
        string channel = null,
        char chatCommandIdentifier = '!',
        char whisperCommandIdentifier = '!',
        bool autoReListenOnExceptions = true)
    {
        if (channel != null && channel[0] == '#')
            channel = channel.Substring(1);
        ConnectionCredentials credentials1 = credentials;
        List<string> channels = new List<string>();
        channels.Add(channel);
        int chatCommandIdentifier1 = (int)chatCommandIdentifier;
        int whisperCommandIdentifier1 = (int)whisperCommandIdentifier;
        int num = autoReListenOnExceptions ? 1 : 0;
        this.initializeHelper(credentials1, channels, (char)chatCommandIdentifier1, (char)whisperCommandIdentifier1,
            num != 0);
    }

    public void Initialize(
        ConnectionCredentials credentials,
        List<string> channels,
        char chatCommandIdentifier = '!',
        char whisperCommandIdentifier = '!',
        bool autoReListenOnExceptions = true)
    {
        channels = channels.Select<string, string>((Func<string, string>)(x => x[0] != '#' ? x : x.Substring(1)))
            .ToList<string>();
        this.initializeHelper(credentials, channels, chatCommandIdentifier, whisperCommandIdentifier,
            autoReListenOnExceptions);
    }

    private void initializeHelper(
        ConnectionCredentials credentials,
        List<string> channels,
        char chatCommandIdentifier = '!',
        char whisperCommandIdentifier = '!',
        bool autoReListenOnExceptions = true)
    {
        this.Log(string.Format("TwitchLib-TwitchClient initialized, assembly version: {0}",
            (object)Assembly.GetExecutingAssembly().GetName().Version));
        this.ConnectionCredentials = credentials;
        this.TwitchUsername = this.ConnectionCredentials.TwitchUsername;
        if (chatCommandIdentifier != char.MinValue)
            this._chatCommandIdentifiers.Add(chatCommandIdentifier);
        if (whisperCommandIdentifier != char.MinValue)
            this._whisperCommandIdentifiers.Add(whisperCommandIdentifier);
        this.AutoReListenOnException = autoReListenOnExceptions;
        if (channels != null && channels.Count > 0)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                if (!string.IsNullOrEmpty(channels[i]))
                {
                    if (this.JoinedChannels.FirstOrDefault<JoinedChannel>(
                            (Func<JoinedChannel, bool>)(x => x.Channel.ToLower() == channels[i])) != null)
                        return;
                    this._joinChannelQueue.Enqueue(new JoinedChannel(channels[i]));
                }
            }
        }

        this.InitializeClient();
    }

    private void InitializeClient()
    {
        if (this._client == null)
        {
            switch (this._protocol)
            {
                case ClientProtocol.TCP:
                    this._client = (IClient)new TcpClient();
                    break;
                case ClientProtocol.WebSocket:
                    this._client = (IClient)new WebSocketClient();
                    break;
            }
        }

        this._client.OnConnected += new EventHandler<OnConnectedEventArgs>(this._client_OnConnected);
        this._client.OnMessage += new EventHandler<OnMessageEventArgs>(this._client_OnMessage);
        this._client.OnDisconnected += new EventHandler<OnDisconnectedEventArgs>(this._client_OnDisconnected);
        this._client.OnFatality += new EventHandler<OnFatalErrorEventArgs>(this._client_OnFatality);
        this._client.OnMessageThrottled +=
            new EventHandler<OnMessageThrottledEventArgs>(this._client_OnMessageThrottled);
        this._client.OnWhisperThrottled +=
            new EventHandler<OnWhisperThrottledEventArgs>(this._client_OnWhisperThrottled);
        this._client.OnReconnected += new EventHandler<OnReconnectedEventArgs>(this._client_OnReconnected);
    }

    internal void RaiseEvent(string eventName, object args = null)
    {
        foreach (Delegate invocation in (this.GetType()
                     .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                     .GetValue((object)this) as MulticastDelegate).GetInvocationList())
        {
            MethodInfo method = invocation.Method;
            object target = invocation.Target;
            object[] parameters;
            if (args != null)
                parameters = new object[2] { (object)this, args };
            else
                parameters = new object[2]
                {
                    (object)this,
                    (object)new EventArgs()
                };
            method.Invoke(target, parameters);
        }
    }

    public void SendRaw(string message)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this.Log("Writing: " + message);
        this._client.Send(message);
        EventHandler<OnSendReceiveDataArgs> onSendReceiveData = this.OnSendReceiveData;
        if (onSendReceiveData == null)
            return;
        onSendReceiveData((object)this, new OnSendReceiveDataArgs()
        {
            Direction = SendReceiveDirection.Sent,
            Data = message
        });
    }

    private void SendTwitchMessage(
        JoinedChannel channel,
        string message,
        string replyToId = null,
        bool dryRun = false)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        if (((channel == null ? 1 : (message == null ? 1 : 0)) | (dryRun ? 1 : 0)) != 0)
            return;
        if (message.Length > 500)
        {
            this.LogError("Message length has exceeded the maximum character count. (500)");
        }
        else
        {
            OutboundChatMessage outboundChatMessage = new OutboundChatMessage()
            {
                Channel = channel.Channel,
                Username = this.ConnectionCredentials.TwitchUsername,
                Message = message
            };
            if (replyToId != null)
                outboundChatMessage.ReplyToId = replyToId;
            this._lastMessageSent = message;
            this._client.Send(outboundChatMessage.ToString());
        }
    }

    public void SendMessage(JoinedChannel channel, string message, bool dryRun = false)
    {
        this.SendTwitchMessage(channel, message, dryRun: dryRun);
    }

    public void SendMessage(string channel, string message, bool dryRun = false)
    {
        this.SendMessage(this.GetJoinedChannel(channel), message, dryRun);
    }

    public void SendReply(JoinedChannel channel, string replyToId, string message, bool dryRun = false)
    {
        this.SendTwitchMessage(channel, message, replyToId, dryRun);
    }

    public void SendReply(string channel, string replyToId, string message, bool dryRun = false)
    {
        this.SendReply(this.GetJoinedChannel(channel), replyToId, message, dryRun);
    }

    public void SendWhisper(string receiver, string message, bool dryRun = false)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        if (dryRun)
            return;
        this._client.SendWhisper(new OutboundWhisperMessage()
        {
            Receiver = receiver,
            Username = this.ConnectionCredentials.TwitchUsername,
            Message = message
        }.ToString());
        EventHandler<OnWhisperSentArgs> onWhisperSent = this.OnWhisperSent;
        if (onWhisperSent == null)
            return;
        onWhisperSent((object)this, new OnWhisperSentArgs()
        {
            Receiver = receiver,
            Message = message
        });
    }

    public bool Connect()
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this.Log("Connecting to: " + this.ConnectionCredentials.TwitchWebsocketURI);
        this._joinedChannelManager.Clear();
        if (!this._client.Open())
            return false;
        this.Log("Should be connected!");
        return true;
    }

    public void Disconnect()
    {
        this.Log("Disconnect Twitch Chat Client...");
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._client.Close();
        this._joinedChannelManager.Clear();
        this.PreviousWhisper = (WhisperMessage)null;
    }

    public void Reconnect()
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this.Log("Reconnecting to Twitch");
        this._client.Reconnect();
    }

    public void AddChatCommandIdentifier(char identifier)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._chatCommandIdentifiers.Add(identifier);
    }

    public void RemoveChatCommandIdentifier(char identifier)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._chatCommandIdentifiers.Remove(identifier);
    }

    public void AddWhisperCommandIdentifier(char identifier)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._whisperCommandIdentifiers.Add(identifier);
    }

    public void RemoveWhisperCommandIdentifier(char identifier)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._whisperCommandIdentifiers.Remove(identifier);
    }

    public void SetConnectionCredentials(ConnectionCredentials credentials)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        if (this.IsConnected)
            throw new IllegalAssignmentException(
                "While the client is connected, you are unable to change the connection credentials. Please disconnect first and then change them.");
        this.ConnectionCredentials = credentials;
    }

    public void JoinChannel(string channel, bool overrideCheck = false)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        if (!this.IsConnected)
            CustomTwitchClient.HandleNotConnected();
        if (this.JoinedChannels.FirstOrDefault<JoinedChannel>(
                (Func<JoinedChannel, bool>)(x => x.Channel.ToLower() == channel && !overrideCheck)) != null)
            return;
        if (channel[0] == '#')
            channel = channel.Substring(1);
        this._joinChannelQueue.Enqueue(new JoinedChannel(channel));
        if (this._currentlyJoiningChannels)
            return;
        this.QueueingJoinCheck();
    }

    public JoinedChannel GetJoinedChannel(string channel)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        if (this.JoinedChannels.Count == 0)
            throw new BadStateException("Must be connected to at least one channel.");
        if (channel[0] == '#')
            channel = channel.Substring(1);
        return this._joinedChannelManager.GetJoinedChannel(channel);
    }

    public void LeaveChannel(string channel)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        channel = channel.ToLower();
        if (channel[0] == '#')
            channel = channel.Substring(1);
        this.Log("Leaving channel: " + channel);
        if (this._joinedChannelManager.GetJoinedChannel(channel) == null)
            return;
        this._client.Send(Rfc2812.Part("#" + channel));
    }

    public void LeaveChannel(JoinedChannel channel)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this.LeaveChannel(channel.Channel);
    }

    public void OnReadLineTest(string rawIrc)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this.HandleIrcMessage(this._ircParser.ParseIrcMessage(rawIrc));
    }

    private void _client_OnWhisperThrottled(object sender, OnWhisperThrottledEventArgs e)
    {
        EventHandler<OnWhisperThrottledEventArgs> whisperThrottled = this.OnWhisperThrottled;
        if (whisperThrottled == null)
            return;
        whisperThrottled(sender, e);
    }

    private void _client_OnMessageThrottled(object sender, OnMessageThrottledEventArgs e)
    {
        EventHandler<OnMessageThrottledEventArgs> messageThrottled = this.OnMessageThrottled;
        if (messageThrottled == null)
            return;
        messageThrottled(sender, e);
    }

    private void _client_OnFatality(object sender, OnFatalErrorEventArgs e)
    {
        EventHandler<OnConnectionErrorArgs> onConnectionError = this.OnConnectionError;
        if (onConnectionError == null)
            return;
        onConnectionError((object)this, new OnConnectionErrorArgs()
        {
            BotUsername = this.TwitchUsername,
            Error = new ErrorEvent() { Message = e.Reason }
        });
    }

    private void _client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
    {
        EventHandler<OnDisconnectedEventArgs> onDisconnected = this.OnDisconnected;
        if (onDisconnected == null)
            return;
        onDisconnected(sender, e);
    }

    private void _client_OnReconnected(object sender, OnReconnectedEventArgs e)
    {
        foreach (JoinedChannel joinedChannel in (IEnumerable<JoinedChannel>)this._joinedChannelManager
                     .GetJoinedChannels())
        {
            if (!string.Equals(joinedChannel.Channel, this.TwitchUsername,
                    StringComparison.CurrentCultureIgnoreCase))
                this._joinChannelQueue.Enqueue(joinedChannel);
        }

        this._joinedChannelManager.Clear();
        EventHandler<OnReconnectedEventArgs> onReconnected = this.OnReconnected;
        if (onReconnected == null)
            return;
        onReconnected(sender, e);
    }

    private void _client_OnMessage(object sender, OnMessageEventArgs e)
    {
        string[] separator = new string[1] { "\r\n" };
        foreach (string raw in e.Message.Split(separator, StringSplitOptions.None))
        {
            if (raw.Length > 1)
            {
                this.Log("Received: " + raw);
                EventHandler<OnSendReceiveDataArgs> onSendReceiveData = this.OnSendReceiveData;
                if (onSendReceiveData != null)
                    onSendReceiveData((object)this, new OnSendReceiveDataArgs()
                    {
                        Direction = SendReceiveDirection.Received,
                        Data = raw
                    });
                this.HandleIrcMessage(this._ircParser.ParseIrcMessage(raw));
            }
        }
    }

    private void _client_OnConnected(object sender, object e)
    {
        this._client.Send(Rfc2812.Pass(this.ConnectionCredentials.TwitchOAuth));
        this._client.Send(Rfc2812.Nick(this.ConnectionCredentials.TwitchUsername));
        this._client.Send(Rfc2812.User(this.ConnectionCredentials.TwitchUsername, 0,
            this.ConnectionCredentials.TwitchUsername));
        if (this.ConnectionCredentials.Capabilities.Membership)
            this._client.Send("CAP REQ twitch.tv/membership");
        if (this.ConnectionCredentials.Capabilities.Commands)
            this._client.Send("CAP REQ twitch.tv/commands");
        if (this.ConnectionCredentials.Capabilities.Tags)
            this._client.Send("CAP REQ twitch.tv/tags");
        if (this._joinChannelQueue == null || this._joinChannelQueue.Count <= 0)
            return;
        this.QueueingJoinCheck();
    }

    private void QueueingJoinCheck()
    {
        if (this._joinChannelQueue.Count > 0)
        {
            this._currentlyJoiningChannels = true;
            JoinedChannel joinedChannel = this._joinChannelQueue.Dequeue();
            this.Log("Joining channel: " + joinedChannel.Channel);
            this._client.Send(Rfc2812.Join("#" + joinedChannel.Channel.ToLower()));
            this._joinedChannelManager.AddJoinedChannel(new JoinedChannel(joinedChannel.Channel));
            this.StartJoinedChannelTimer(joinedChannel.Channel);
        }
        else
            this.Log("Finished channel joining queue.");
    }
    
    private void StartJoinedChannelTimer(string channel)
    {
        if (this._joinTimer == null)
        {
            this._joinTimer = new System.Timers.Timer(1000.0);
            this._joinTimer.Elapsed += new ElapsedEventHandler(this.JoinChannelTimeout);
            this._awaitingJoins = new List<KeyValuePair<string, DateTime>>();
        }

        this._awaitingJoins.Add(new KeyValuePair<string, DateTime>(channel.ToLower(), DateTime.Now));
        if (this._joinTimer.Enabled)
            return;
        this._joinTimer.Start();
    }

    private void JoinChannelTimeout(object sender, ElapsedEventArgs e)
    {
        if (this._awaitingJoins.Any<KeyValuePair<string, DateTime>>())
        {
            List<KeyValuePair<string, DateTime>> list = this._awaitingJoins
                .Where<KeyValuePair<string, DateTime>>(
                    (Func<KeyValuePair<string, DateTime>, bool>)(x => (DateTime.Now - x.Value).TotalSeconds > 5.0))
                .ToList<KeyValuePair<string, DateTime>>();
            if (!list.Any<KeyValuePair<string, DateTime>>())
                return;
            this._awaitingJoins.RemoveAll(
                (Predicate<KeyValuePair<string, DateTime>>)(x => (DateTime.Now - x.Value).TotalSeconds > 5.0));
            foreach (KeyValuePair<string, DateTime> keyValuePair in list)
            {
                this._joinedChannelManager.RemoveJoinedChannel(keyValuePair.Key.ToLowerInvariant());
                EventHandler<OnFailureToReceiveJoinConfirmationArgs> joinConfirmation =
                    this.OnFailureToReceiveJoinConfirmation;
                if (joinConfirmation != null)
                    joinConfirmation((object)this, new OnFailureToReceiveJoinConfirmationArgs()
                    {
                        Exception = new FailureToReceiveJoinConfirmationException(keyValuePair.Key)
                    });
            }
        }
        else
        {
            this._joinTimer.Stop();
            this._currentlyJoiningChannels = false;
            this.QueueingJoinCheck();
        }
    }

    private void HandleIrcMessage(IrcMessage ircMessage)
    {
        if (ircMessage.Message.Contains("Login authentication failed"))
        {
            EventHandler<OnIncorrectLoginArgs> onIncorrectLogin = this.OnIncorrectLogin;
            if (onIncorrectLogin == null)
                return;
            onIncorrectLogin((object)this, new OnIncorrectLoginArgs()
            {
                Exception = new ErrorLoggingInException(ircMessage.ToString(), this.TwitchUsername)
            });
        }
        else
        {
            switch (ircMessage.Command)
            {
                case IrcCommand.PrivMsg:
                    this.HandlePrivMsg(ircMessage);
                    break;
                case IrcCommand.Notice:
                    this.HandleNotice(ircMessage);
                    break;
                case IrcCommand.Ping:
                    if (this.DisableAutoPong)
                        break;
                    this.SendRaw("PONG");
                    break;
                case IrcCommand.Pong:
                    break;
                case IrcCommand.Join:
                    this.HandleJoin(ircMessage);
                    break;
                case IrcCommand.Part:
                    this.HandlePart(ircMessage);
                    break;
                case IrcCommand.ClearChat:
                    this.HandleClearChat(ircMessage);
                    break;
                case IrcCommand.ClearMsg:
                    this.HandleClearMsg(ircMessage);
                    break;
                case IrcCommand.UserState:
                    this.HandleUserState(ircMessage);
                    break;
                case IrcCommand.GlobalUserState:
                    break;
                case IrcCommand.Cap:
                    this.HandleCap(ircMessage);
                    break;
                case IrcCommand.RPL_001:
                    break;
                case IrcCommand.RPL_002:
                    break;
                case IrcCommand.RPL_003:
                    break;
                case IrcCommand.RPL_004:
                    this.Handle004();
                    break;
                case IrcCommand.RPL_353:
                    this.Handle353(ircMessage);
                    break;
                case IrcCommand.RPL_366:
                    this.Handle366();
                    break;
                case IrcCommand.RPL_372:
                    break;
                case IrcCommand.RPL_375:
                    break;
                case IrcCommand.RPL_376:
                    break;
                case IrcCommand.Whisper:
                    this.HandleWhisper(ircMessage);
                    break;
                case IrcCommand.RoomState:
                    this.HandleRoomState(ircMessage);
                    break;
                case IrcCommand.Reconnect:
                    this.Reconnect();
                    break;
                case IrcCommand.UserNotice:
                    this.HandleUserNotice(ircMessage);
                    break;
                case IrcCommand.Mode:
                    this.HandleMode(ircMessage);
                    break;
                default:
                    EventHandler<OnUnaccountedForArgs> onUnaccountedFor = this.OnUnaccountedFor;
                    if (onUnaccountedFor != null)
                        onUnaccountedFor((object)this, new OnUnaccountedForArgs()
                        {
                            BotUsername = this.TwitchUsername,
                            Channel = (string)null,
                            Location = nameof(HandleIrcMessage),
                            RawIRC = ircMessage.ToString()
                        });
                    this.UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }

    private void HandlePrivMsg(IrcMessage ircMessage)
    {
        ChatMessage chatMessage = new ChatMessage(this.TwitchUsername, ircMessage, ref this._channelEmotes,
            this.WillReplaceEmotes);
        foreach (JoinedChannel joinedChannel in this.JoinedChannels.Where<JoinedChannel>(
                     (Func<JoinedChannel, bool>)(x => string.Equals(x.Channel, ircMessage.Channel,
                         StringComparison.InvariantCultureIgnoreCase))))
            joinedChannel.HandleMessage(chatMessage);
        EventHandler<OnMessageReceivedArgs> onMessageReceived = this.OnMessageReceived;
        if (onMessageReceived != null)
            onMessageReceived((object)this, new OnMessageReceivedArgs()
            {
                ChatMessage = chatMessage
            });
        string str;
        if (ircMessage.Tags.TryGetValue("msg-id", out str) && str == "user-intro")
        {
            EventHandler<OnUserIntroArgs> onUserIntro = this.OnUserIntro;
            if (onUserIntro != null)
                onUserIntro((object)this, new OnUserIntroArgs()
                {
                    ChatMessage = chatMessage
                });
        }

        if (this._chatCommandIdentifiers == null || this._chatCommandIdentifiers.Count == 0 ||
            string.IsNullOrEmpty(chatMessage.Message) ||
            !this._chatCommandIdentifiers.Contains(chatMessage.Message[0]))
            return;
        ChatCommand chatCommand = new ChatCommand(chatMessage);
        EventHandler<OnChatCommandReceivedArgs> chatCommandReceived = this.OnChatCommandReceived;
        if (chatCommandReceived == null)
            return;
        chatCommandReceived((object)this, new OnChatCommandReceivedArgs()
        {
            Command = chatCommand
        });
    }

    private void HandleNotice(IrcMessage ircMessage)
    {
        if (ircMessage.Message.Contains("Improperly formatted auth"))
        {
            EventHandler<OnIncorrectLoginArgs> onIncorrectLogin = this.OnIncorrectLogin;
            if (onIncorrectLogin == null)
                return;
            onIncorrectLogin((object)this, new OnIncorrectLoginArgs()
            {
                Exception = new ErrorLoggingInException(ircMessage.ToString(), this.TwitchUsername)
            });
        }
        else
        {
            string str;
            if (!ircMessage.Tags.TryGetValue("msg-id", out str))
            {
                EventHandler<OnUnaccountedForArgs> onUnaccountedFor = this.OnUnaccountedFor;
                if (onUnaccountedFor != null)
                    onUnaccountedFor((object)this, new OnUnaccountedForArgs()
                    {
                        BotUsername = this.TwitchUsername,
                        Channel = ircMessage.Channel,
                        Location = "NoticeHandling",
                        RawIRC = ircMessage.ToString()
                    });
                this.UnaccountedFor(ircMessage.ToString());
            }

            switch (str)
            {
                case "color_changed":
                    EventHandler<OnChatColorChangedArgs> chatColorChanged = this.OnChatColorChanged;
                    if (chatColorChanged == null)
                        break;
                    chatColorChanged((object)this, new OnChatColorChangedArgs()
                    {
                        Channel = ircMessage.Channel
                    });
                    break;
                case "msg_banned":
                    EventHandler<OnBannedArgs> onBanned = this.OnBanned;
                    if (onBanned == null)
                        break;
                    onBanned((object)this, new OnBannedArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_banned_email_alias":
                    EventHandler<OnBannedEmailAliasArgs> bannedEmailAlias = this.OnBannedEmailAlias;
                    if (bannedEmailAlias == null)
                        break;
                    bannedEmailAlias((object)this, new OnBannedEmailAliasArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_channel_suspended":
                    this._awaitingJoins.RemoveAll(
                        (Predicate<KeyValuePair<string, DateTime>>)(x => x.Key.ToLower() == ircMessage.Channel));
                    this._joinedChannelManager.RemoveJoinedChannel(ircMessage.Channel);
                    this.QueueingJoinCheck();
                    EventHandler<OnFailureToReceiveJoinConfirmationArgs> joinConfirmation =
                        this.OnFailureToReceiveJoinConfirmation;
                    if (joinConfirmation == null)
                        break;
                    joinConfirmation((object)this, new OnFailureToReceiveJoinConfirmationArgs()
                    {
                        Exception = new FailureToReceiveJoinConfirmationException(ircMessage.Channel,
                            ircMessage.Message)
                    });
                    break;
                case "msg_duplicate":
                    EventHandler<OnDuplicateArgs> onDuplicate = this.OnDuplicate;
                    if (onDuplicate == null)
                        break;
                    onDuplicate((object)this, new OnDuplicateArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_emoteonly":
                    EventHandler<OnEmoteOnlyArgs> onEmoteOnly = this.OnEmoteOnly;
                    if (onEmoteOnly == null)
                        break;
                    onEmoteOnly((object)this, new OnEmoteOnlyArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_followersonly":
                    EventHandler<OnFollowersOnlyArgs> onFollowersOnly = this.OnFollowersOnly;
                    if (onFollowersOnly == null)
                        break;
                    onFollowersOnly((object)this, new OnFollowersOnlyArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_r9k":
                    EventHandler<OnR9kModeArgs> onR9kMode = this.OnR9kMode;
                    if (onR9kMode == null)
                        break;
                    onR9kMode((object)this, new OnR9kModeArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_ratelimit":
                    EventHandler<OnRateLimitArgs> onRateLimit = this.OnRateLimit;
                    if (onRateLimit == null)
                        break;
                    onRateLimit((object)this, new OnRateLimitArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_requires_verified_phone_number":
                    EventHandler<OnRequiresVerifiedPhoneNumberArgs> verifiedPhoneNumber =
                        this.OnRequiresVerifiedPhoneNumber;
                    if (verifiedPhoneNumber == null)
                        break;
                    verifiedPhoneNumber((object)this, new OnRequiresVerifiedPhoneNumberArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_slowmode":
                    EventHandler<OnSlowModeArgs> onSlowMode = this.OnSlowMode;
                    if (onSlowMode == null)
                        break;
                    onSlowMode((object)this, new OnSlowModeArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_subsonly":
                    EventHandler<OnSubsOnlyArgs> onSubsOnly = this.OnSubsOnly;
                    if (onSubsOnly == null)
                        break;
                    onSubsOnly((object)this, new OnSubsOnlyArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_suspended":
                    EventHandler<OnSuspendedArgs> onSuspended = this.OnSuspended;
                    if (onSuspended == null)
                        break;
                    onSuspended((object)this, new OnSuspendedArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "msg_verified_email":
                    EventHandler<OnRequiresVerifiedEmailArgs> requiresVerifiedEmail = this.OnRequiresVerifiedEmail;
                    if (requiresVerifiedEmail == null)
                        break;
                    requiresVerifiedEmail((object)this, new OnRequiresVerifiedEmailArgs()
                    {
                        Channel = ircMessage.Channel,
                        Message = ircMessage.Message
                    });
                    break;
                case "no_mods":
                    EventHandler<OnModeratorsReceivedArgs> moderatorsReceived1 = this.OnModeratorsReceived;
                    if (moderatorsReceived1 == null)
                        break;
                    moderatorsReceived1((object)this, new OnModeratorsReceivedArgs()
                    {
                        Channel = ircMessage.Channel,
                        Moderators = new List<string>()
                    });
                    break;
                case "no_permission":
                    EventHandler noPermissionError = this.OnNoPermissionError;
                    if (noPermissionError == null)
                        break;
                    noPermissionError((object)this, (EventArgs)null);
                    break;
                case "no_vips":
                    EventHandler<OnVIPsReceivedArgs> onViPsReceived1 = this.OnVIPsReceived;
                    if (onViPsReceived1 == null)
                        break;
                    onViPsReceived1((object)this, new OnVIPsReceivedArgs()
                    {
                        Channel = ircMessage.Channel,
                        VIPs = new List<string>()
                    });
                    break;
                case "raid_error_self":
                    EventHandler onSelfRaidError = this.OnSelfRaidError;
                    if (onSelfRaidError == null)
                        break;
                    onSelfRaidError((object)this, (EventArgs)null);
                    break;
                case "raid_notice_mature":
                    EventHandler isMatureAudience = this.OnRaidedChannelIsMatureAudience;
                    if (isMatureAudience == null)
                        break;
                    isMatureAudience((object)this, (EventArgs)null);
                    break;
                case "room_mods":
                    EventHandler<OnModeratorsReceivedArgs> moderatorsReceived2 = this.OnModeratorsReceived;
                    if (moderatorsReceived2 == null)
                        break;
                    moderatorsReceived2((object)this, new OnModeratorsReceivedArgs()
                    {
                        Channel = ircMessage.Channel,
                        Moderators =
                            ((IEnumerable<string>)ircMessage.Message.Replace(" ", "").Split(':')[1].Split(','))
                            .ToList<string>()
                    });
                    break;
                case "vips_success":
                    EventHandler<OnVIPsReceivedArgs> onViPsReceived2 = this.OnVIPsReceived;
                    if (onViPsReceived2 == null)
                        break;
                    onViPsReceived2((object)this, new OnVIPsReceivedArgs()
                    {
                        Channel = ircMessage.Channel,
                        VIPs =
                            ((IEnumerable<string>)ircMessage.Message.Replace(" ", "").Replace(".", "").Split(':')[1]
                                .Split(',')).ToList<string>()
                    });
                    break;
                default:
                    EventHandler<OnUnaccountedForArgs> onUnaccountedFor1 = this.OnUnaccountedFor;
                    if (onUnaccountedFor1 != null)
                        onUnaccountedFor1((object)this, new OnUnaccountedForArgs()
                        {
                            BotUsername = this.TwitchUsername,
                            Channel = ircMessage.Channel,
                            Location = "NoticeHandling",
                            RawIRC = ircMessage.ToString()
                        });
                    this.UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }

    private void HandleJoin(IrcMessage ircMessage)
    {
        EventHandler<OnUserJoinedArgs> onUserJoined = this.OnUserJoined;
        if (onUserJoined == null)
            return;
        onUserJoined((object)this, new OnUserJoinedArgs()
        {
            Channel = ircMessage.Channel,
            Username = ircMessage.User
        });
    }

    private void HandlePart(IrcMessage ircMessage)
    {
        if (string.Equals(this.TwitchUsername, ircMessage.User, StringComparison.InvariantCultureIgnoreCase))
        {
            this._joinedChannelManager.RemoveJoinedChannel(ircMessage.Channel);
            this._hasSeenJoinedChannels.Remove(ircMessage.Channel);
            EventHandler<OnLeftChannelArgs> onLeftChannel = this.OnLeftChannel;
            if (onLeftChannel == null)
                return;
            onLeftChannel((object)this, new OnLeftChannelArgs()
            {
                BotUsername = this.TwitchUsername,
                Channel = ircMessage.Channel
            });
        }
        else
        {
            EventHandler<OnUserLeftArgs> onUserLeft = this.OnUserLeft;
            if (onUserLeft == null)
                return;
            onUserLeft((object)this, new OnUserLeftArgs()
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
            EventHandler<OnChatClearedArgs> onChatCleared = this.OnChatCleared;
            if (onChatCleared == null)
                return;
            onChatCleared((object)this, new OnChatClearedArgs()
            {
                Channel = ircMessage.Channel
            });
        }
        else if (ircMessage.Tags.TryGetValue("ban-duration", out string _))
        {
            UserTimeout userTimeout = new UserTimeout(ircMessage);
            EventHandler<OnUserTimedoutArgs> onUserTimedout = this.OnUserTimedout;
            if (onUserTimedout == null)
                return;
            onUserTimedout((object)this, new OnUserTimedoutArgs()
            {
                UserTimeout = userTimeout
            });
        }
        else
        {
            UserBan userBan = new UserBan(ircMessage);
            EventHandler<OnUserBannedArgs> onUserBanned = this.OnUserBanned;
            if (onUserBanned == null)
                return;
            onUserBanned((object)this, new OnUserBannedArgs()
            {
                UserBan = userBan
            });
        }
    }

    private void HandleClearMsg(IrcMessage ircMessage)
    {
        EventHandler<OnMessageClearedArgs> onMessageCleared = this.OnMessageCleared;
        if (onMessageCleared == null)
            return;
        onMessageCleared((object)this, new OnMessageClearedArgs()
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
        if (!this._hasSeenJoinedChannels.Contains(state.Channel.ToLowerInvariant()))
        {
            this._hasSeenJoinedChannels.Add(state.Channel.ToLowerInvariant());
            EventHandler<OnUserStateChangedArgs> userStateChanged = this.OnUserStateChanged;
            if (userStateChanged == null)
                return;
            userStateChanged((object)this, new OnUserStateChangedArgs()
            {
                UserState = state
            });
        }
        else
        {
            EventHandler<OnMessageSentArgs> onMessageSent = this.OnMessageSent;
            if (onMessageSent == null)
                return;
            onMessageSent((object)this, new OnMessageSentArgs()
            {
                SentMessage = new SentMessage(state, this._lastMessageSent)
            });
        }
    }

    private void Handle004()
    {
        EventHandler<OnConnectedArgs> onConnected = this.OnConnected;
        if (onConnected == null)
            return;
        onConnected((object)this, new OnConnectedArgs()
        {
            BotUsername = this.TwitchUsername
        });
    }
    
    private void Handle353(IrcMessage ircMessage)
    {
        EventHandler<OnExistingUsersDetectedArgs> existingUsersDetected = this.OnExistingUsersDetected;
        if (existingUsersDetected == null)
            return;
        existingUsersDetected((object)this, new OnExistingUsersDetectedArgs()
        {
            Channel = ircMessage.Channel,
            Users = ((IEnumerable<string>)ircMessage.Message.Split(' ')).ToList<string>()
        });
    }

    private void Handle366()
    {
        this._currentlyJoiningChannels = false;
        this.QueueingJoinCheck();
    }
    
    private void HandleWhisper(IrcMessage ircMessage)
    {
        WhisperMessage whisperMessage = new WhisperMessage(ircMessage, this.TwitchUsername);
        this.PreviousWhisper = whisperMessage;
        EventHandler<OnWhisperReceivedArgs> onWhisperReceived = this.OnWhisperReceived;
        if (onWhisperReceived != null)
            onWhisperReceived((object)this, new OnWhisperReceivedArgs()
            {
                WhisperMessage = whisperMessage
            });
        if (this._whisperCommandIdentifiers != null && this._whisperCommandIdentifiers.Count != 0 &&
            !string.IsNullOrEmpty(whisperMessage.Message) &&
            this._whisperCommandIdentifiers.Contains(whisperMessage.Message[0]))
        {
            WhisperCommand whisperCommand = new WhisperCommand(whisperMessage);
            EventHandler<OnWhisperCommandReceivedArgs> whisperCommandReceived = this.OnWhisperCommandReceived;
            if (whisperCommandReceived == null)
                return;
            whisperCommandReceived((object)this, new OnWhisperCommandReceivedArgs()
            {
                Command = whisperCommand
            });
        }
        else
        {
            EventHandler<OnUnaccountedForArgs> onUnaccountedFor = this.OnUnaccountedFor;
            if (onUnaccountedFor != null)
                onUnaccountedFor((object)this, new OnUnaccountedForArgs()
                {
                    BotUsername = this.TwitchUsername,
                    Channel = ircMessage.Channel,
                    Location = "WhispergHandling",
                    RawIRC = ircMessage.ToString()
                });
            this.UnaccountedFor(ircMessage.ToString());
        }
    }
    
    private void HandleRoomState(IrcMessage ircMessage)
    {
        if (ircMessage.Tags.Count > 2)
        {
            this._awaitingJoins.Remove(
                this._awaitingJoins.FirstOrDefault<KeyValuePair<string, DateTime>>(
                    (Func<KeyValuePair<string, DateTime>, bool>)(x => x.Key == ircMessage.Channel)));
            EventHandler<OnJoinedChannelArgs> onJoinedChannel = this.OnJoinedChannel;
            if (onJoinedChannel != null)
                onJoinedChannel((object)this, new OnJoinedChannelArgs()
                {
                    BotUsername = this.TwitchUsername,
                    Channel = ircMessage.Channel
                });
        }

        EventHandler<OnChannelStateChangedArgs> channelStateChanged = this.OnChannelStateChanged;
        if (channelStateChanged == null)
            return;
        channelStateChanged((object)this, new OnChannelStateChangedArgs()
        {
            ChannelState = new ChannelState(ircMessage),
            Channel = ircMessage.Channel
        });
    }
    
    private void HandleUserNotice(IrcMessage ircMessage)
    {
        string str;
        if (!ircMessage.Tags.TryGetValue("msg-id", out str))
        {
            EventHandler<OnUnaccountedForArgs> onUnaccountedFor = this.OnUnaccountedFor;
            if (onUnaccountedFor != null)
                onUnaccountedFor((object)this, new OnUnaccountedForArgs()
                {
                    BotUsername = this.TwitchUsername,
                    Channel = ircMessage.Channel,
                    Location = "UserNoticeHandling",
                    RawIRC = ircMessage.ToString()
                });
            this.UnaccountedFor(ircMessage.ToString());
        }
        else
        {
            switch (str)
            {
                case "announcement":
                    Announcement announcement = new Announcement(ircMessage);
                    EventHandler<OnAnnouncementArgs> onAnnouncement = this.OnAnnouncement;
                    if (onAnnouncement == null)
                        break;
                    onAnnouncement((object)this, new OnAnnouncementArgs()
                    {
                        Announcement = announcement,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "giftpaidupgrade":
                    ContinuedGiftedSubscription giftedSubscription1 = new ContinuedGiftedSubscription(ircMessage);
                    EventHandler<OnContinuedGiftedSubscriptionArgs> giftedSubscription2 =
                        this.OnContinuedGiftedSubscription;
                    if (giftedSubscription2 == null)
                        break;
                    giftedSubscription2((object)this, new OnContinuedGiftedSubscriptionArgs()
                    {
                        ContinuedGiftedSubscription = giftedSubscription1,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "primepaidupgrade":
                    PrimePaidSubscriber primePaidSubscriber1 = new PrimePaidSubscriber(ircMessage);
                    EventHandler<OnPrimePaidSubscriberArgs> primePaidSubscriber2 = this.OnPrimePaidSubscriber;
                    if (primePaidSubscriber2 == null)
                        break;
                    primePaidSubscriber2((object)this, new OnPrimePaidSubscriberArgs()
                    {
                        PrimePaidSubscriber = primePaidSubscriber1,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "raid":
                    RaidNotification raidNotification1 = new RaidNotification(ircMessage);
                    EventHandler<OnRaidNotificationArgs> raidNotification2 = this.OnRaidNotification;
                    if (raidNotification2 == null)
                        break;
                    raidNotification2((object)this, new OnRaidNotificationArgs()
                    {
                        Channel = ircMessage.Channel,
                        RaidNotification = raidNotification1
                    });
                    break;
                case "resub":
                    ReSubscriber reSubscriber = new ReSubscriber(ircMessage);
                    EventHandler<OnReSubscriberArgs> onReSubscriber = this.OnReSubscriber;
                    if (onReSubscriber == null)
                        break;
                    onReSubscriber((object)this, new OnReSubscriberArgs()
                    {
                        ReSubscriber = reSubscriber,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "sub":
                    Subscriber subscriber = new Subscriber(ircMessage);
                    EventHandler<OnNewSubscriberArgs> onNewSubscriber = this.OnNewSubscriber;
                    if (onNewSubscriber == null)
                        break;
                    onNewSubscriber((object)this, new OnNewSubscriberArgs()
                    {
                        Subscriber = subscriber,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "subgift":
                    GiftedSubscription giftedSubscription3 = new GiftedSubscription(ircMessage);
                    EventHandler<OnGiftedSubscriptionArgs> giftedSubscription4 = this.OnGiftedSubscription;
                    if (giftedSubscription4 == null)
                        break;
                    giftedSubscription4((object)this, new OnGiftedSubscriptionArgs()
                    {
                        GiftedSubscription = giftedSubscription3,
                        Channel = ircMessage.Channel
                    });
                    break;
                case "submysterygift":
                    CommunitySubscription communitySubscription1 = new CommunitySubscription(ircMessage);
                    EventHandler<OnCommunitySubscriptionArgs> communitySubscription2 = this.OnCommunitySubscription;
                    if (communitySubscription2 == null)
                        break;
                    communitySubscription2((object)this, new OnCommunitySubscriptionArgs()
                    {
                        GiftedSubscription = communitySubscription1,
                        Channel = ircMessage.Channel
                    });
                    break;
                default:
                    EventHandler<OnUnaccountedForArgs> onUnaccountedFor1 = this.OnUnaccountedFor;
                    if (onUnaccountedFor1 != null)
                        onUnaccountedFor1((object)this, new OnUnaccountedForArgs()
                        {
                            BotUsername = this.TwitchUsername,
                            Channel = ircMessage.Channel,
                            Location = "UserNoticeHandling",
                            RawIRC = ircMessage.ToString()
                        });
                    this.UnaccountedFor(ircMessage.ToString());
                    break;
            }
        }
    }
    
    private void HandleMode(IrcMessage ircMessage)
    {
        if (ircMessage.Message.StartsWith("+o"))
        {
            EventHandler<OnModeratorJoinedArgs> onModeratorJoined = this.OnModeratorJoined;
            if (onModeratorJoined == null)
                return;
            onModeratorJoined((object)this, new OnModeratorJoinedArgs()
            {
                Channel = ircMessage.Channel,
                Username = ircMessage.Message.Split(' ')[1]
            });
        }
        else
        {
            if (!ircMessage.Message.StartsWith("-o"))
                return;
            EventHandler<OnModeratorLeftArgs> onModeratorLeft = this.OnModeratorLeft;
            if (onModeratorLeft == null)
                return;
            onModeratorLeft((object)this, new OnModeratorLeftArgs()
            {
                Channel = ircMessage.Channel,
                Username = ircMessage.Message.Split(' ')[1]
            });
        }
    }
    
    private void HandleCap(IrcMessage ircMessage)
    {
    }

    private void UnaccountedFor(string ircString)
    {
        this.Log("Unaccounted for: " + ircString + " (please create a TwitchLib GitHub issue :P)");
    }
    
    private void Log(string message, bool includeDate = false, bool includeTime = false)
    {
        string str = !(includeDate & includeTime)
            ? (!includeDate ? DateTime.UtcNow.ToShortTimeString() ?? "" : DateTime.UtcNow.ToShortDateString() ?? "")
            : string.Format("{0}", (object)DateTime.UtcNow);
        if (includeDate | includeTime)
        {
            ILogger<CustomTwitchClient> logger = this._logger;
            if (logger != null)
                logger.LogInformation(string.Format("[TwitchLib, {0} - {1}] {2}",
                    (object)Assembly.GetExecutingAssembly().GetName().Version, (object)str, (object)message));
        }
        else
        {
            ILogger<CustomTwitchClient> logger = this._logger;
            if (logger != null)
                logger.LogInformation(string.Format("[TwitchLib, {0}] {1}",
                    (object)Assembly.GetExecutingAssembly().GetName().Version, (object)message));
        }

        EventHandler<OnLogArgs> onLog = this.OnLog;
        if (onLog == null)
            return;
        onLog((object)this, new OnLogArgs()
        {
            BotUsername = this.ConnectionCredentials?.TwitchUsername,
            Data = message,
            DateTime = DateTime.UtcNow
        });
    }
    
    private void LogError(string message, bool includeDate = false, bool includeTime = false)
    {
        string str = !(includeDate & includeTime)
            ? (!includeDate ? DateTime.UtcNow.ToShortTimeString() ?? "" : DateTime.UtcNow.ToShortDateString() ?? "")
            : string.Format("{0}", (object)DateTime.UtcNow);
        if (includeDate | includeTime)
        {
            ILogger<CustomTwitchClient> logger = this._logger;
            if (logger != null)
                logger.LogError(string.Format("[TwitchLib, {0} - {1}] {2}",
                    (object)Assembly.GetExecutingAssembly().GetName().Version, (object)str, (object)message));
        }
        else
        {
            ILogger<CustomTwitchClient> logger = this._logger;
            if (logger != null)
                logger.LogError(string.Format("[TwitchLib, {0}] {1}",
                    (object)Assembly.GetExecutingAssembly().GetName().Version, (object)message));
        }

        EventHandler<OnLogArgs> onLog = this.OnLog;
        if (onLog == null)
            return;
        onLog((object)this, new OnLogArgs()
        {
            BotUsername = this.ConnectionCredentials?.TwitchUsername,
            Data = message,
            DateTime = DateTime.UtcNow
        });
    }
    
    public void SendQueuedItem(string message)
    {
        if (!this.IsInitialized)
            CustomTwitchClient.HandleNotInitialized();
        this._client.Send(message);
    }

    protected static void HandleNotInitialized()
    {
        throw new ClientNotInitializedException(
            "The twitch client has not been initialized and cannot be used. Please call Initialize();");
    }

    protected static void HandleNotConnected()
    {
        throw new ClientNotConnectedException(
            "In order to perform this action, the client must be connected to Twitch. To confirm connection, try performing this action in or after the OnConnected event has been fired.");
    }
}