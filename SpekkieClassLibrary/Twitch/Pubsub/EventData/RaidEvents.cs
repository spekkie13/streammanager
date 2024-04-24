using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class RaidEvents : MessageData
{
    public RaidType Type { get; protected set; }

    public Guid Id { get; protected set; }

    public string ChannelId { get; protected set; }

    public string TargetChannelId { get; protected set; }

    public string TargetLogin { get; protected set; }

    public string TargetDisplayName { get; protected set; }

    public string TargetProfileImage { get; protected set; }

    public DateTime AnnounceTime { get; protected set; }

    public DateTime RaidTime { get; protected set; }

    public int RemainigDurationSeconds { get; protected set; }

    public int ViewerCount { get; protected set; }

    public RaidEvents(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        switch (jtoken.SelectToken("type").ToString())
        {
            case "raid_update":
                Type = RaidType.RaidUpdate;
                break;
            case "raid_update_v2":
                Type = RaidType.RaidUpdateV2;
                break;
            case "raid_go_v2":
                Type = RaidType.RaidGo;
                break;
        }

        switch (Type)
        {
            case RaidType.RaidUpdate:
                Id = Guid.Parse(jtoken.SelectToken("raid.id").ToString());
                ChannelId = jtoken.SelectToken("raid.source_id").ToString();
                TargetChannelId = jtoken.SelectToken("raid.target_id").ToString();
                AnnounceTime = DateTime.Parse(jtoken.SelectToken("raid.announce_time").ToString());
                RaidTime = DateTime.Parse(jtoken.SelectToken("raid.raid_time").ToString());
                RemainigDurationSeconds =
                    int.Parse(jtoken.SelectToken("raid.remaining_duration_seconds").ToString());
                ViewerCount = int.Parse(jtoken.SelectToken("raid.viewer_count").ToString());
                break;
            case RaidType.RaidUpdateV2:
                Id = Guid.Parse(jtoken.SelectToken("raid.id").ToString());
                ChannelId = jtoken.SelectToken("raid.source_id").ToString();
                TargetChannelId = jtoken.SelectToken("raid.target_id").ToString();
                TargetLogin = jtoken.SelectToken("raid.target_login").ToString();
                TargetDisplayName = jtoken.SelectToken("raid.target_display_name").ToString();
                TargetProfileImage = jtoken.SelectToken("raid.target_profile_image").ToString();
                ViewerCount = int.Parse(jtoken.SelectToken("raid.viewer_count").ToString());
                break;
            case RaidType.RaidGo:
                Id = Guid.Parse(jtoken.SelectToken("raid.id").ToString());
                ChannelId = jtoken.SelectToken("raid.source_id").ToString();
                TargetChannelId = jtoken.SelectToken("raid.target_id").ToString();
                TargetLogin = jtoken.SelectToken("raid.target_login").ToString();
                TargetDisplayName = jtoken.SelectToken("raid.target_display_name").ToString();
                TargetProfileImage = jtoken.SelectToken("raid.target_profile_image").ToString();
                ViewerCount = int.Parse(jtoken.SelectToken("raid.viewer_count").ToString());
                break;
        }
    }
}