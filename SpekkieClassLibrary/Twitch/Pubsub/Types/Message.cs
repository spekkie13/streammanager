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

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class Message
{
    public readonly MessageData MessageData;
    public string Topic { get; }

    public Message(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr).SelectToken("data");
        Topic = jtoken?.SelectToken("topic")?.ToString();
        string jsonStr1 = jtoken?.SelectToken("message")?.ToString();
        string topic = Topic;
        string str;
        if (topic == null)
            str = null;
        else
            str = topic.Split('.')[0];
        string s = str;
        switch (ComputeStringHash(s))
        {
            case 450000440:
                if (s != "automod-queue")
                    break;
                MessageData = new AutomodQueue(jsonStr1);
                break;
            case 522816415:
                if (s != "channel-bits-events-v1")
                    break;
                MessageData = new ChannelBitsEvents(jsonStr1);
                break;
            case 539594034:
                if (s != "channel-bits-events-v2")
                    break;
                MessageData =
                    JsonConvert.DeserializeObject<ChannelBitsEventsV2>(
                        JObject.Parse(jsonStr1?.Replace("\\", ""))["data"]?.ToString() ?? "");
                break;
            case 778451386:
                if (s != "whispers")
                    break;
                MessageData = new Whisper(jsonStr1);
                break;
            case 1212123455:
                if (s != "predictions-channel-v1")
                    break;
                MessageData = new PredictionEvents(jsonStr1);
                break;
            case 1248430604:
                if (s != "leaderboard-events-v1")
                    break;
                MessageData = new LeaderboardEvents(jsonStr1);
                break;
            case 1266735245:
                if (s != "channel-subscribe-events-v1")
                    break;
                MessageData = new ChannelSubscription(jsonStr1);
                break;
            case 1970825802:
                if (s != "video-playback-by-id")
                    break;
                MessageData = new VideoPlayback(jsonStr1);
                break;
            case 2101714332:
                if (s != "channel-points-channel-v1")
                    break;
                MessageData = new ChannelPointsChannel(jsonStr1);
                break;
            case 2157984858:
                if (s != "community-points-channel-v1")
                    break;
                MessageData = new CommunityPointsChannel(jsonStr1);
                break;
            case 2476983697:
                if (s != "raid")
                    break;
                MessageData = new RaidEvents(jsonStr1);
                break;
            case 2535512472:
                if (s != "following")
                    break;
                MessageData = new Following(jsonStr1);
                break;
            case 2643987228:
                if (s != "user-moderation-notifications")
                    break;
                MessageData = new UserModerationNotifications(jsonStr1);
                break;
            case 3075833323:
                if (s != "chat_moderator_actions")
                    break;
                MessageData = new ChatModeratorActions(jsonStr1);
                break;
            case 3729941884:
                if (s != "channel-ext-v1")
                    break;
                MessageData = new ChannelExtensionBroadcast(jsonStr1);
                break;
        }
    }
    
    internal static uint ComputeStringHash(string s)
    {
        const uint StringHash = new();
        return s?.Aggregate(2166136261U, (current, t) => (uint)((t ^ (int)current) * 16777619)) ?? StringHash;
    }
}