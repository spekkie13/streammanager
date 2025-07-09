using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class ChannelPointsChannel : MessageData
{
    public ChannelPointsChannel(string jsonStr)
    {
        RawData = jsonStr;
        JToken jtoken = JObject.Parse(jsonStr);
        if (jtoken.SelectToken("type")?.ToString() == "reward-redeemed")
        {
            Type = ChannelPointsChannelType.RewardRedeemed;
            Data = JsonConvert.DeserializeObject<RewardRedeemed>(jtoken.SelectToken("data")?.ToString() ?? "");
        }
        else
        {
            Type = ChannelPointsChannelType.Unknown;
        }
    }

    public ChannelPointsChannelType Type { get; private set; }

    public ChannelPointsData? Data { get; private set; }

    public string? RawData { get; private set; }
}