#nullable disable
using SpekkieClassLibrary.Twitch.Pubsub.Args;
using TwitchLib.PubSub.Events;
using OnChannelSubscriptionArgs = SpekkieClassLibrary.Twitch.Pubsub.Events.Args.OnChannelSubscriptionArgs;
using OnListenResponseArgs = SpekkieClassLibrary.Twitch.Pubsub.Args.OnListenResponseArgs;
using OnPredictionArgs = SpekkieClassLibrary.Twitch.Pubsub.Events.Args.OnPredictionArgs;
using OnRewardRedeemedArgs = TwitchLib.PubSub.Events.OnRewardRedeemedArgs;
using OnWhisperArgs = SpekkieClassLibrary.Twitch.Pubsub.Events.Args.OnWhisperArgs;

namespace SpekkieClassLibrary.Twitch.Pubsub.Interfaces;

public interface ITwitchPubSub
{
    event EventHandler<OnBanArgs> OnBan;
    event EventHandler<OnBitsReceivedArgs> OnBitsReceived;
    event EventHandler<OnChannelExtensionBroadcastArgs> OnChannelExtensionBroadcast;
    event EventHandler<OnChannelSubscriptionArgs> OnChannelSubscription;
    event EventHandler<OnClearArgs> OnClear;
    event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly;
    event EventHandler<OnEmoteOnlyOffArgs> OnEmoteOnlyOff;
    event EventHandler<OnFollowArgs> OnFollow;
    event EventHandler<OnHostArgs> OnHost;
    event EventHandler<OnMessageDeletedArgs> OnMessageDeleted;
    event EventHandler<OnListenResponseArgs> OnListenResponse;
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
    event EventHandler<OnWhisperArgs> OnWhisper;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    event EventHandler<OnCustomRewardCreatedArgs> OnCustomRewardCreated;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    event EventHandler<OnCustomRewardUpdatedArgs> OnCustomRewardUpdated;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    event EventHandler<OnCustomRewardDeletedArgs> OnCustomRewardDeleted;
    [Obsolete("This event fires on an undocumented/retired/obsolete topic.", false)]
    event EventHandler<OnRewardRedeemedArgs> OnRewardRedeemed;
    event EventHandler<ChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
    event EventHandler<OnLeaderboardEventArgs> OnLeaderboardSubs;
    event EventHandler<OnLeaderboardEventArgs> OnLeaderboardBits;
    event EventHandler<OnRaidUpdateArgs> OnRaidUpdate;
    event EventHandler<OnRaidUpdateV2Args> OnRaidUpdateV2;
    event EventHandler<OnRaidGoArgs> OnRaidGo;
    event EventHandler<OnLogArgs> OnLog;
    event EventHandler<OnCommercialArgs> OnCommercial;
    event EventHandler<OnPredictionArgs> OnPrediction;
    
    void Connect();
    void Disconnect();
    [Obsolete("This topic is deprecated by Twitch. Please use ListenToBitsEventsV2()", false)]
    void ListenToBitsEvents(string channelTwitchId);
    void ListenToChannelExtensionBroadcast(string channelId, string extensionId);
    void ListenToChatModeratorActions(string myTwitchId, string channelTwitchId);
    void ListenToFollows(string channelId);
    void ListenToSubscriptions(string channelId);
    void ListenToVideoPlayback(string channelName);
    void ListenToWhispers(string channelTwitchId);
    [Obsolete("This method listens to an undocumented/retired/obsolete topic. Consider using ListenToChannelPoints()",
        false)]
    void ListenToRewards(string channelTwitchId);
    void ListenToChannelPoints(string channelTwitchId);
    void ListenToLeaderboards(string channelTwitchId);
    void ListenToRaid(string channelTwitchId);
    void ListenToPredictions(string channelTwitchId);
    void SendTopics(string oauth = null, bool unlisten = false);
    void TestMessageParser(string testJsonString);
}