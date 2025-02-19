#nullable disable
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
        switch (jtoken.SelectToken("identifier.domain")?.ToString())
        {
            case "bits-usage-by-channel-v1":
                Type = LeaderBoardType.BitsUsageByChannel;
                break;
            case "sub-gift-sent":
                Type = LeaderBoardType.SubGiftSent;
                break;
        }

        switch (Type)
        {
            case LeaderBoardType.BitsUsageByChannel:
                var channelId = jtoken.SelectToken("identifier.grouping_key")?.ToString() ?? "";
                ChannelId = channelId;
                using (var enumerator = jtoken["top"]?.Children().GetEnumerator())
                {
                    if (enumerator == null) return;
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(current?.SelectToken("rank")?.ToString() ?? ""),
                            Score = int.Parse(current?.SelectToken("score")?.ToString() ?? ""),
                            UserId = current?.SelectToken("entry_key")?.ToString()
                        });
                    }

                    break;
                }
            case LeaderBoardType.SubGiftSent:
                channelId = jtoken.SelectToken("identifier.grouping_key")?.ToString() ?? "";
                ChannelId = channelId;
                using (var enumerator = jtoken["top"]?.Children().GetEnumerator())
                {
                    if (enumerator == null) return;
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(current?.SelectToken("rank")?.ToString() ?? ""),
                            Score = int.Parse(current?.SelectToken("score")?.ToString() ?? ""),
                            UserId = current?.SelectToken("entry_key")?.ToString()
                        });
                    }

                    break;
                }
        }
    }

    public LeaderBoardType Type { get; }
    public string ChannelId { get; private set; }
    public List<LeaderBoard> Top { get; } = new();
}