using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using TwitchLib.PubSub.Models;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class LeaderboardEvents : MessageData
{
    public LeaderboardEvents(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        Type = jtoken.SelectToken("identifier.domain")?.ToString() switch
        {
            "bits-usage-by-channel-v1" => LeaderBoardType.BitsUsageByChannel,
            "sub-gift-sent" => LeaderBoardType.SubGiftSent,
            _ => Type
        };

        switch (Type)
        {
            case LeaderBoardType.BitsUsageByChannel:
                string channelId = jtoken.SelectToken("identifier.grouping_key")?.ToString() ?? "";
                ChannelId = channelId;
                using (IEnumerator<JToken>? enumerator = jtoken["top"]?.Children().GetEnumerator())
                {
                    if (enumerator == null) return;
                    while (enumerator.MoveNext())
                    {
                        JToken current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(current.SelectToken("rank")?.ToString() ?? ""),
                            Score = int.Parse(current.SelectToken("score")?.ToString() ?? ""),
                            UserId = current.SelectToken("entry_key")?.ToString()
                        });
                    }

                    break;
                }
            case LeaderBoardType.SubGiftSent:
                channelId = jtoken.SelectToken("identifier.grouping_key")?.ToString() ?? "";
                ChannelId = channelId;
                using (IEnumerator<JToken>? enumerator = jtoken["top"]?.Children().GetEnumerator())
                {
                    if (enumerator == null) return;
                    while (enumerator.MoveNext())
                    {
                        JToken current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(current.SelectToken("rank")?.ToString() ?? ""),
                            Score = int.Parse(current.SelectToken("score")?.ToString() ?? ""),
                            UserId = current.SelectToken("entry_key")?.ToString()
                        });
                    }

                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public LeaderBoardType Type { get; }
    public string ChannelId { get; private set; }
    public List<LeaderBoard> Top { get; } = [];
}