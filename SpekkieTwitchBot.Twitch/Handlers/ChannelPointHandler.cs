using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General.FileHandling;
using Redemption = SpekkieClassLibrary.Twitch.Events.ChannelPoint.Redemption;

namespace TwitchAuthService.Handlers;

public class ChannelPointHandler(CustomTwitchHttpClient client, Logger logger)
{
    public HttpResponseMessage? HandleSongRedemption(bool success, string redemptionId, string rewardId)
    {
        string status = success
            ? TwitchConstants.ChannelPointStatusFulfilled
            : TwitchConstants.ChannelPointStatusCancelled;
        
        HttpResponseMessage? message = UpdateRedemptionStatus(redemptionId, TwitchConstants.BroadcasterId, rewardId, status).Result;
        return message;
    }

    public async Task<Redemption> GetMostRecentRedemptionForUser(string username)
    {
        List<ChannelPointData> redemption = await GetCustomRedemptions();
        List<string?> redemptionIds = redemption.Select(r => r.Id).ToList();
        if (redemptionIds.Count == 0) return Redemption.Empty;
        
        List<Redemption> outstandingRedemptions = [];
        foreach (string? id in redemptionIds)
        {
            Redemption[] redemptionData = await GetRedemptionsByStatus(id);
            outstandingRedemptions.AddRange(redemptionData.ToList());
        }
        
        outstandingRedemptions.Sort((r1, r2) => DateTime.Compare(DateTime.Parse(r1.RedeemedAt!), DateTime.Parse(r2.RedeemedAt!)));
        Redemption red = outstandingRedemptions.First(red => red.UserName == username);
        return red;
    }

    public string CreateRedemption(string commandArgs)
    {
        string title = commandArgs.Split("|")[0];
        string prompt = commandArgs.Split("|")[1];
        int cost = Convert.ToInt32(commandArgs.Split("|")[2]);
        bool isUserInputRequired = true;

        if (commandArgs.Split("|").Length > 3)
            isUserInputRequired = Convert.ToInt32(commandArgs.Split("|").Last()) != 0;
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}30731359";
        string rewardInfo =
            $"{{\"title\":\"{title}\",\"cost\":{cost},\"is_user_input_required\":{isUserInputRequired.ToString().ToLower()},\"prompt\":\"{prompt[..Math.Min(prompt.Length, 200)]}\"}}";
        StringContent content = new (rewardInfo, Encoding.UTF8, "application/json");

        HttpResponseMessage response = client.PostAsync(url, content).Result;
        string responseMessage = response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}";
        logger.LogInfo(responseMessage);
        return responseMessage;
    }

    public async Task<List<ChannelPointData>> GetCustomRedemptions()
    {
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await client.GetAsync(url);
        if (!message.IsSuccessStatusCode) return [];
        
        string response = await message.Content.ReadAsStringAsync();
        ChannelPointRequest redemptions =
            JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return [];
        List<ChannelPointData> rewardIds = redemptions.Data.ToList();
        return rewardIds;
    }

    private async Task<Redemption[]> GetRedemptionsByStatus(string? rewardId)
    {
        if (string.IsNullOrEmpty(rewardId)) return [];
        string url =
            $"{TwitchConstants.TwitchChannelRedemptionsUrl}{TwitchConstants.BroadcasterId}&reward_id={rewardId}&status={TwitchConstants.ChannelPointStatusUncompleted}";

        HttpResponseMessage message = await client.GetAsync(url);
        if (!message.IsSuccessStatusCode) return [];

        string response = await message.Content.ReadAsStringAsync();
        CpRewardRequest? unfulfilled = JsonConvert.DeserializeObject<CpRewardRequest>(response);
        return unfulfilled?.Data ?? [];
    }

    public async Task<HttpResponseMessage?> UpdateRedemptionStatus(
        string? id,
        string? broadcasterId,
        string? rewardId,
        string? status)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(rewardId) || string.IsNullOrEmpty(status))
            return null;
        
        StringContent requestContent = new ($"{{\"status\":\"{status}\"}}",
            Encoding.UTF8, mediaType: "application/json");

        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{broadcasterId}&reward_id={rewardId}&id={id}";
        HttpResponseMessage message = await client.PatchAsync(requestUrl, requestContent);
        return message;
    }
}