#nullable disable
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class CommunityPointsChannel : MessageData
{
    public CommunityPointsChannel(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        Type = jtoken.SelectToken("type")?.ToString() switch
        {
            "reward-redeemed" or "redemption-status-update" => CommunityPointsChannelType.RewardRedeemed,
            "custom-reward-created" => CommunityPointsChannelType.CustomRewardCreated,
            "custom-reward-updated" => CommunityPointsChannelType.CustomRewardUpdated,
            "custom-reward-deleted" => CommunityPointsChannelType.CustomRewardDeleted,
            _ => ~CommunityPointsChannelType.RewardRedeemed
        };

        TimeStamp = DateTime.Parse(jtoken.SelectToken("data.timestamp")?.ToString() ?? "");
        switch (Type)
        {
            case CommunityPointsChannelType.RewardRedeemed:
                ChannelId = jtoken.SelectToken("data.redemption.channel_id")?.ToString();
                Login = jtoken.SelectToken("data.redemption.user.login")?.ToString();
                DisplayName = jtoken.SelectToken("data.redemption.user.display_name")?.ToString();
                RewardId = Guid.Parse(jtoken.SelectToken("data.redemption.reward.id")?.ToString() ?? "");
                RewardTitle = jtoken.SelectToken("data.redemption.reward.title")?.ToString();
                RewardPrompt = jtoken.SelectToken("data.redemption.reward.prompt")?.ToString();
                RewardCost = int.Parse(jtoken.SelectToken("data.redemption.reward.cost")?.ToString() ?? "");
                Message = jtoken.SelectToken("data.redemption.user_input")?.ToString();
                Status = jtoken.SelectToken("data.redemption.status")?.ToString();
                RedemptionId = Guid.Parse(jtoken.SelectToken("data.redemption.id")?.ToString() ?? "");
                break;
            case CommunityPointsChannelType.CustomRewardUpdated:
                ChannelId = jtoken.SelectToken("data.updated_reward.channel_id")?.ToString();
                RewardId = Guid.Parse(jtoken.SelectToken("data.updated_reward.id")?.ToString() ?? "");
                RewardTitle = jtoken.SelectToken("data.updated_reward.title")?.ToString();
                RewardPrompt = jtoken.SelectToken("data.updated_reward.prompt")?.ToString();
                RewardCost = int.Parse(jtoken.SelectToken("data.updated_reward.cost")?.ToString() ?? "");
                break;
            case CommunityPointsChannelType.CustomRewardCreated:
                ChannelId = jtoken.SelectToken("data.new_reward.channel_id")?.ToString();
                RewardId = Guid.Parse(jtoken.SelectToken("data.new_reward.id")?.ToString() ?? "");
                RewardTitle = jtoken.SelectToken("data.new_reward.title")?.ToString();
                RewardPrompt = jtoken.SelectToken("data.new_reward.prompt")?.ToString();
                RewardCost = int.Parse(jtoken.SelectToken("data.new_reward.cost")?.ToString() ?? "");
                break;
            case CommunityPointsChannelType.CustomRewardDeleted:
                ChannelId = jtoken.SelectToken("data.deleted_reward.channel_id")?.ToString();
                RewardId = Guid.Parse(jtoken.SelectToken("data.deleted_reward.id")?.ToString() ?? "");
                RewardTitle = jtoken.SelectToken("data.deleted_reward.title")?.ToString();
                RewardPrompt = jtoken.SelectToken("data.deleted_reward.prompt")?.ToString();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

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
}