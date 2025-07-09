using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class RaidEvents : MessageData
{
    public RaidEvents(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        Type = jtoken.SelectToken("type")?.ToString() switch
        {
            "raid_update" => RaidType.RaidUpdate,
            "raid_update_v2" => RaidType.RaidUpdateV2,
            "raid_go_v2" => RaidType.RaidGo,
            _ => Type
        };

        switch (Type)
        {
            case RaidType.RaidUpdate:
            case RaidType.RaidUpdateV2:
            case RaidType.RaidGo:
                Id = Guid.Parse(jtoken.SelectToken("raid.id")?.ToString() ?? "");
                ChannelId = jtoken.SelectToken("raid.source_id")?.ToString() ?? "";
                TargetChannelId = jtoken.SelectToken("raid.target_id")?.ToString() ?? "";
                AnnounceTime = DateTime.Parse(jtoken.SelectToken("raid.announce_time")?.ToString() ?? "");
                RaidTime = DateTime.Parse(jtoken.SelectToken("raid.raid_time")?.ToString() ?? "");
                RemainingDurationSeconds =
                    int.Parse(jtoken.SelectToken("raid.remaining_duration_seconds")?.ToString() ?? "0");
                ViewerCount = int.Parse(jtoken.SelectToken("raid.viewer_count")?.ToString() ?? "0");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public RaidType Type { get; protected set; }

    public Guid Id { get; protected set; }

    public string? ChannelId { get; protected set; }

    public string? TargetChannelId { get; protected set; }

    public string? TargetLogin { get; protected set; }

    public string? TargetDisplayName { get; protected set; }

    public string? TargetProfileImage { get; protected set; }

    public DateTime AnnounceTime { get; protected set; }

    public DateTime RaidTime { get; protected set; }

    public int RemainingDurationSeconds { get; protected set; }

    public int ViewerCount { get; protected set; }
}