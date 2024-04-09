using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace SpekkieTwitchBot.Twitch.Events.Interfaces
{
  public interface ICustomTwitchClient
  {
    bool AutoReListenOnException { get; set; }
    MessageEmoteCollection ChannelEmotes { get; }
    ConnectionCredentials ConnectionCredentials { get; }
    bool DisableAutoPong { get; set; }
    bool IsConnected { get; }
    IReadOnlyList<JoinedChannel> JoinedChannels { get; }
    WhisperMessage? PreviousWhisper { get; }
    string TwitchUsername { get; }
    bool WillReplaceEmotes { get; set; }
    
    event EventHandler<OnChannelStateChangedArgs> OnChannelStateChanged;
    event EventHandler<OnChatClearedArgs> OnChatCleared;
    event EventHandler<OnChatColorChangedArgs> OnChatColorChanged;
    event EventHandler<OnChatCommandReceivedArgs> OnChatCommandReceived;
    event EventHandler<OnConnectedArgs> OnConnected;
    event EventHandler<OnConnectionErrorArgs> OnConnectionError;
    event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
    event EventHandler<OnExistingUsersDetectedArgs> OnExistingUsersDetected;
    event EventHandler<OnGiftedSubscriptionArgs> OnGiftedSubscription;
    event EventHandler<OnIncorrectLoginArgs> OnIncorrectLogin;
    event EventHandler<OnJoinedChannelArgs> OnJoinedChannel;
    event EventHandler<OnLeftChannelArgs> OnLeftChannel;
    event EventHandler<OnLogArgs> OnLog;
    event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
    event EventHandler<OnMessageSentArgs> OnMessageSent;
    event EventHandler<OnModeratorJoinedArgs> OnModeratorJoined;
    event EventHandler<OnModeratorLeftArgs> OnModeratorLeft;
    event EventHandler<OnModeratorsReceivedArgs> OnModeratorsReceived;
    event EventHandler<OnNewSubscriberArgs> OnNewSubscriber;
    event EventHandler<OnRaidNotificationArgs> OnRaidNotification;
    event EventHandler<OnReSubscriberArgs> OnReSubscriber;
    event EventHandler<OnSendReceiveDataArgs> OnSendReceiveData;
    event EventHandler<OnUserBannedArgs> OnUserBanned;
    event EventHandler<OnUserJoinedArgs> OnUserJoined;
    event EventHandler<OnUserLeftArgs> OnUserLeft;
    event EventHandler<OnUserStateChangedArgs> OnUserStateChanged;
    event EventHandler<OnUserTimedoutArgs> OnUserTimedout;
    event EventHandler<OnWhisperCommandReceivedArgs> OnWhisperCommandReceived;
    event EventHandler<OnWhisperReceivedArgs> OnWhisperReceived;
    event EventHandler<OnWhisperSentArgs> OnWhisperSent;
    event EventHandler<OnMessageThrottledEventArgs> OnMessageThrottled;
    event EventHandler<OnWhisperThrottledEventArgs> OnWhisperThrottled;
    event EventHandler<OnErrorEventArgs> OnError;
    event EventHandler<OnReconnectedEventArgs> OnReconnected;
    event EventHandler<OnVIPsReceivedArgs> OnVIPsReceived;
    event EventHandler<OnCommunitySubscriptionArgs> OnCommunitySubscription;
    event EventHandler<OnMessageClearedArgs> OnMessageCleared;
    event EventHandler<OnRequiresVerifiedEmailArgs> OnRequiresVerifiedEmail;
    event EventHandler<OnRequiresVerifiedPhoneNumberArgs> OnRequiresVerifiedPhoneNumber;
    event EventHandler<OnBannedEmailAliasArgs> OnBannedEmailAlias;
    event EventHandler<OnUserIntroArgs> OnUserIntro;
    event EventHandler<OnAnnouncementArgs> OnAnnouncement;

    void Initialize(
      ConnectionCredentials credentials,
      string channel = "",
      char chatCommandIdentifier = '!',
      char whisperCommandIdentifier = '!',
      bool autoReListenOnExceptions = true);

    void Initialize(ConnectionCredentials credentials,
      List<string> channels,
      char chatCommandIdentifier = '!',
      char whisperCommandIdentifier = '!',
      bool autoReListenOnExceptions = true);

    void SetConnectionCredentials(ConnectionCredentials credentials);

    void AddChatCommandIdentifier(char identifier);

    void AddWhisperCommandIdentifier(char identifier);

    void RemoveChatCommandIdentifier(char identifier);

    void RemoveWhisperCommandIdentifier(char identifier);

    bool Connect();

    void Disconnect();

    void Reconnect();

    JoinedChannel? GetJoinedChannel(string channel);

    void JoinChannel(string channel, bool overrideCheck = false);

    void LeaveChannel(JoinedChannel channel);

    void LeaveChannel(string channel);

    void OnReadLineTest(string rawIrc);

    void SendMessage(JoinedChannel? channel, string? message, bool dryRun = false);

    void SendMessage(string channel, string? message, bool dryRun = false);

    void SendReply(JoinedChannel channel, string? replyToId, string? message, bool dryRun = false);

    void SendReply(string channel, string? replyToId, string? message, bool dryRun = false);

    void SendQueuedItem(string message);

    void SendRaw(string message);

    void SendWhisper(string receiver, string message, bool dryRun = false);
  }
}
