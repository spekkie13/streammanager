using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChannelBitsEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelBitsEvents;
using ChannelBitsEventsV2 = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelBitsEventsV2;
using ChannelExtensionBroadcast = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChannelExtensionBroadcast;
using ChatModeratorActions = SpekkieClassLibrary.Twitch.Pubsub.EventData.ChatModeratorActions;
using LeaderboardEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.LeaderboardEvents;
using MessageData = SpekkieClassLibrary.Twitch.Pubsub.Abstract.MessageData;
using PredictionEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.PredictionEvents;
using RaidEvents = SpekkieClassLibrary.Twitch.Pubsub.EventData.RaidEvents;
using UserModerationNotifications = SpekkieClassLibrary.Twitch.Pubsub.EventData.UserModerationNotifications;
using Whisper = SpekkieClassLibrary.Twitch.Pubsub.EventData.Whisper;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class Message
{
    public readonly MessageData? MessageData;

    public Message(string jsonStr)
    {
        JToken? jToken = JObject.Parse(jsonStr).SelectToken("data");
        if (jToken == null || string.IsNullOrEmpty(jToken.SelectToken("topic")?.ToString())) return;
        Topic = jToken.SelectToken("topic")?.ToString();
        if (string.IsNullOrEmpty(Topic) || string.IsNullOrEmpty(jToken.SelectToken("message")?.ToString())) return;
        string jsonStr1 = jToken.SelectToken("message")?.ToString() ?? "";
        string topic = Topic;
        string? str = topic?.Split('.')[0];
        switch (ComputeStringHash(str))
        {
            case 450000440:
                if (str != "automod-queue")
                    break;
                MessageData = new AutomodQueue(jsonStr1);
                break;
            case 522816415:
                if (str != "channel-bits-events-v1")
                    break;
                MessageData = new ChannelBitsEvents(jsonStr1);
                break;
            case 539594034:
                if (str != "channel-bits-events-v2")
                    break;
                MessageData =
                    JsonConvert.DeserializeObject<ChannelBitsEventsV2>(
                        JObject.Parse(jsonStr1.Replace("\\", ""))["data"]?.ToString() ?? "");
                break;
            case 778451386:
                if (str != "whispers")
                    break;
                MessageData = new Whisper(jsonStr1);
                break;
            case 1212123455:
                if (str != "predictions-channel-v1")
                    break;
                MessageData = new PredictionEvents(jsonStr1);
                break;
            case 1248430604:
                if (str != "leaderboard-events-v1")
                    break;
                MessageData = new LeaderboardEvents(jsonStr1);
                break;
            case 1266735245:
                if (str != "channel-subscribe-events-v1")
                    break;
                MessageData = new ChannelSubscription(jsonStr1);
                break;
            case 1970825802:
                if (str != "video-playback-by-id")
                    break;
                MessageData = new VideoPlayback(jsonStr1);
                break;
            case 2101714332:
                if (str != "channel-points-channel-v1")
                    break;
                MessageData = new ChannelPointsChannel(jsonStr1);
                break;
            case 2157984858:
                if (str != "community-points-channel-v1")
                    break;
                MessageData = new CommunityPointsChannel(jsonStr1);
                break;
            case 2476983697:
                if (str != "raid")
                    break;
                MessageData = new RaidEvents(jsonStr1);
                break;
            case 2535512472:
                if (str != "following")
                    break;
                MessageData = new Following(jsonStr1);
                break;
            case 2643987228:
                if (str != "user-moderation-notifications")
                    break;
                MessageData = new UserModerationNotifications(jsonStr1);
                break;
            case 3075833323:
                if (str != "chat_moderator_actions")
                    break;
                MessageData = new ChatModeratorActions(jsonStr1);
                break;
            case 3729941884:
                if (str != "channel-ext-v1")
                    break;
                MessageData = new ChannelExtensionBroadcast(jsonStr1);
                break;
        }
    }

    public string? Topic { get; }

    private static uint ComputeStringHash(string? s)
    {
        const uint StringHash = 0;
        return s?.Aggregate(2166136261U, (current, t) => (uint)((t ^ (int)current) * 16777619)) ?? StringHash;
    }
}