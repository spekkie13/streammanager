using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using TwitchLib.PubSub.Models;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class LeaderboardEvents : MessageData
{
    public LeaderBoardType Type { get; private set; }
    public string ChannelId { get; private set; }
    public List<LeaderBoard> Top { get; private set; } = new();
    
    public LeaderboardEvents(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        switch (((object)jtoken.SelectToken("identifier.domain")).ToString())
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
                ChannelId = ((object)jtoken.SelectToken("identifier.grouping_key")).ToString();
                using (IEnumerator<JToken> enumerator = jtoken["top"].Children().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        JToken current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(((object)current.SelectToken("rank")).ToString()),
                            Score = int.Parse(((object)current.SelectToken("score")).ToString()),
                            UserId = ((object)current.SelectToken("entry_key")).ToString()
                        });
                    }

                    break;
                }
            case LeaderBoardType.SubGiftSent:
                ChannelId = ((object)jtoken.SelectToken("identifier.grouping_key")).ToString();
                using (IEnumerator<JToken> enumerator = jtoken["top"].Children().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        JToken current = enumerator.Current;
                        Top.Add(new LeaderBoard
                        {
                            Place = int.Parse(((object)current.SelectToken("rank")).ToString()),
                            Score = int.Parse(((object)current.SelectToken("score")).ToString()),
                            UserId = ((object)current.SelectToken("entry_key")).ToString()
                        });
                    }

                    break;
                }
        }
    }
}