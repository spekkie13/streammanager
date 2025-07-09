#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;
using TwitchLib.PubSub.Events;

namespace TwitchAuthService.Events.Interfaces;

public interface ITwitchPubSub
{
    event EventHandler<OnBanArgs> OnBan;
    event EventHandler<OnBitsReceivedArgs> OnBitsReceived;
    event EventHandler<OnChannelExtensionBroadcastArgs> OnChannelExtensionBroadcast;
    event EventHandler<ChannelSubscriptionArgs> OnChannelSubscription;
    event EventHandler<OnClearArgs> OnClear;
    event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly;
    event EventHandler<OnEmoteOnlyOffArgs> OnEmoteOnlyOff;
    event EventHandler<OnFollowArgs> OnFollow;
    event EventHandler<OnHostArgs> OnHost;
    event EventHandler<OnMessageDeletedArgs> OnMessageDeleted;
    event EventHandler<ListenResponseArgs> OnListenResponse;
    event EventHandler OnPubSubServiceClosed;
    event EventHandler OnPubSubServiceConnected;
    event EventHandler<OnPubSubServiceErrorArgs> OnPubSubServiceError;
    event EventHandler<OnStreamDownArgs> OnStreamDown;
    event EventHandler<OnStreamUpArgs> OnStreamUp;
    event EventHandler<OnSubscribersOnlyArgs> OnSubscribersOnly;
    event EventHandler<OnSubscribersOnlyOffArgs> OnSubscribersOnlyOff;
    event EventHandler<OnTimeoutArgs> OnTimeout;
    event EventHandler<OnUnbanArgs> OnUnban;
    event EventHandler<OnUntimeoutArgs> OnUntimeout;
    event EventHandler<OnViewCountArgs> OnViewCount;
    event EventHandler<WhisperArgs> OnWhisper;
    event EventHandler<ChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
    event EventHandler<OnLeaderboardEventArgs> OnLeaderboardSubs;
    event EventHandler<OnLeaderboardEventArgs> OnLeaderboardBits;
    event EventHandler<OnRaidUpdateArgs> OnRaidUpdate;
    event EventHandler<OnRaidUpdateV2Args> OnRaidUpdateV2;
    event EventHandler<OnRaidGoArgs> OnRaidGo;
    event EventHandler<OnLogArgs> OnLog;
    event EventHandler<OnCommercialArgs> OnCommercial;
    event EventHandler<PredictionArgs> OnPrediction;

    void Connect();
    void Disconnect();
    void ListenToChannelExtensionBroadcast(string channelId, string extensionId);
    void ListenToChatModeratorActions(string myTwitchId, string channelTwitchId);
    void ListenToFollows(string channelId);
    void ListenToSubscriptions(string channelId);
    void ListenToVideoPlayback(string channelName);
    void ListenToWhispers(string channelTwitchId);
    void ListenToChannelPoints(string channelTwitchId);
    void ListenToLeaderboards(string channelTwitchId);
    void ListenToRaid(string channelTwitchId);
    void ListenToPredictions(string channelTwitchId);
    void SendTopics(string oauth = null, bool unlisten = false);
    void TestMessageParser(string testJsonString);
}