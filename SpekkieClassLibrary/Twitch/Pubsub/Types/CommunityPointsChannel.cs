using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class CommunityPointsChannel : MessageData
{
    public CommunityPointsChannelType Type { get; protected set; }
    public DateTime TimeStamp { get; protected set; }
    public string ChannelId { get; protected set; }
    public string Login { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Message { get; protected set; }
    public Guid RewardId { get; protected set; }
    public string RewardTitle { get; protected set; }
    public string RewardPrompt { get; protected set; }
    public int RewardCost { get; protected set; }
    public string Status { get; protected set; }
    public Guid RedemptionId { get; protected set; }

    public CommunityPointsChannel(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        switch (((object)jtoken.SelectToken("type")).ToString())
        {
            case "reward-redeemed":
            case "redemption-status-update":
                Type = CommunityPointsChannelType.RewardRedeemed;
                break;
            case "custom-reward-created":
                Type = CommunityPointsChannelType.CustomRewardCreated;
                break;
            case "custom-reward-updated":
                Type = CommunityPointsChannelType.CustomRewardUpdated;
                break;
            case "custom-reward-deleted":
                Type = CommunityPointsChannelType.CustomRewardDeleted;
                break;
            default:
                Type = ~CommunityPointsChannelType.RewardRedeemed;
                break;
        }

        TimeStamp = DateTime.Parse(((object)jtoken.SelectToken("data.timestamp")).ToString());
        switch (Type)
        {
            case CommunityPointsChannelType.RewardRedeemed:
                ChannelId = ((object)jtoken.SelectToken("data.redemption.channel_id")).ToString();
                Login = ((object)jtoken.SelectToken("data.redemption.user.login")).ToString();
                DisplayName = ((object)jtoken.SelectToken("data.redemption.user.display_name")).ToString();
                RewardId = Guid.Parse(((object)jtoken.SelectToken("data.redemption.reward.id")).ToString());
                RewardTitle = ((object)jtoken.SelectToken("data.redemption.reward.title")).ToString();
                RewardPrompt = ((object)jtoken.SelectToken("data.redemption.reward.prompt")).ToString();
                RewardCost = int.Parse(((object)jtoken.SelectToken("data.redemption.reward.cost")).ToString());
                Message = ((object)jtoken.SelectToken("data.redemption.user_input"))?.ToString();
                Status = ((object)jtoken.SelectToken("data.redemption.status")).ToString();
                RedemptionId = Guid.Parse(((object)jtoken.SelectToken("data.redemption.id")).ToString());
                break;
            case CommunityPointsChannelType.CustomRewardUpdated:
                ChannelId = ((object)jtoken.SelectToken("data.updated_reward.channel_id")).ToString();
                RewardId = Guid.Parse(((object)jtoken.SelectToken("data.updated_reward.id")).ToString());
                RewardTitle = ((object)jtoken.SelectToken("data.updated_reward.title")).ToString();
                RewardPrompt = ((object)jtoken.SelectToken("data.updated_reward.prompt")).ToString();
                RewardCost = int.Parse(((object)jtoken.SelectToken("data.updated_reward.cost")).ToString());
                break;
            case CommunityPointsChannelType.CustomRewardCreated:
                ChannelId = ((object)jtoken.SelectToken("data.new_reward.channel_id")).ToString();
                RewardId = Guid.Parse(((object)jtoken.SelectToken("data.new_reward.id")).ToString());
                RewardTitle = ((object)jtoken.SelectToken("data.new_reward.title")).ToString();
                RewardPrompt = ((object)jtoken.SelectToken("data.new_reward.prompt")).ToString();
                RewardCost = int.Parse(((object)jtoken.SelectToken("data.new_reward.cost")).ToString());
                break;
            case CommunityPointsChannelType.CustomRewardDeleted:
                ChannelId = ((object)jtoken.SelectToken("data.deleted_reward.channel_id")).ToString();
                RewardId = Guid.Parse(((object)jtoken.SelectToken("data.deleted_reward.id")).ToString());
                RewardTitle = ((object)jtoken.SelectToken("data.deleted_reward.title")).ToString();
                RewardPrompt = ((object)jtoken.SelectToken("data.deleted_reward.prompt")).ToString();
                break;
        }
    }
}