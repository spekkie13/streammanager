using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class ChannelPointsChannel : MessageData
{
    public ChannelPointsChannelType Type { get; private set; }

    public ChannelPointsData Data { get; private set; }

    public string RawData { get; private set; }

    public ChannelPointsChannel(string jsonStr)
    {
        RawData = jsonStr;
        JToken jtoken = JObject.Parse(jsonStr);
        if (((object) jtoken.SelectToken("type")).ToString() == "reward-redeemed")
        {
            Type = ChannelPointsChannelType.RewardRedeemed;
            Data = (ChannelPointsData) JsonConvert.DeserializeObject<RewardRedeemed>(((object) jtoken.SelectToken("data")).ToString());
        }
        else
            Type = ChannelPointsChannelType.Unknown;
    }
}