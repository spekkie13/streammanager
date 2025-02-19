using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General.FileHandling;
using Redemption = SpekkieClassLibrary.Twitch.Events.ChannelPoint.Redemption;

namespace TwitchAuthService.Handlers;

public class ChannelPointHandler
{
    private readonly Logger _Logger;
    private readonly CustomTwitchHttpClient _TwitchHttpClient;

    public ChannelPointHandler(CustomTwitchHttpClient client, Logger logger)
    {
        _TwitchHttpClient = client;
        _Logger = logger;
    }

    public HttpResponseMessage HandleSongRedemption(bool success, string redemptionId, string rewardId)
    {
        var status = success
            ? TwitchConstants.ChannelPointStatusFulfilled
            : TwitchConstants.ChannelPointStatusCancelled;
        var message =
            UpdateRedemptionStatus(
                redemptionId,
                TwitchConstants.BroadcasterId,
                rewardId,
                status).Result;

        return message;
    }

    public async Task<Redemption> GetMostRecentRedemptionForUser(string username)
    {
        var redemptionIds = await GetCustomRedemptions();
        var outstandingRedemptions = new List<Redemption>();
        foreach (var id in redemptionIds)
        {
            var redemptionData = await GetRedemptionsByStatus(id);
            outstandingRedemptions.AddRange(redemptionData.ToList());
        }

        outstandingRedemptions.Sort((r1, r2) =>
            DateTime.Compare(DateTime.Parse(r1.RedeemedAt), DateTime.Parse(r2.RedeemedAt)));
        var red = outstandingRedemptions.First(red => red.UserName == username);
        return red;
    }

    public void CreateRedemption(string commandArgs)
    {
        var title = commandArgs.Split("|")[0];
        var prompt = commandArgs.Split("|")[1];
        var cost = Convert.ToInt32(commandArgs.Split("|")[2]);
        var isUserInputRequired = true;

        if (commandArgs.Split("|").Length > 3)
            isUserInputRequired = Convert.ToInt32(commandArgs.Split("|").Last()) != 0;
        var url = $"{TwitchConstants.TwitchChannelRewardsUrl}30731359";
        var rewardInfo =
            $"{{\"title\":\"{title}\",\"cost\":{cost},\"is_user_input_required\":{isUserInputRequired.ToString().ToLower()},\"prompt\":\"{prompt.Substring(0, Math.Min(prompt.Length, 200))}\"}}";
        var content = new StringContent(rewardInfo, Encoding.UTF8, "application/json");

        var response = _TwitchHttpClient.PostAsync(url, content).Result;

        _Logger.LogInfo(response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}");
    }

    private async Task<List<string>> GetCustomRedemptions()
    {
        var url = $"{TwitchConstants.TwitchChannelRewardsUrl}{TwitchConstants.BroadcasterId}";
        var message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return new List<string>();
        var response = await message.Content.ReadAsStringAsync();
        var redemptions =
            JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return new List<string>();
        var rewardIds = redemptions.Data.Select(x => x.Id).ToList();
        return rewardIds;
    }

    private async Task<Redemption[]> GetRedemptionsByStatus(string rewardId)
    {
        var url =
            $"{TwitchConstants.TwitchChannelRedemptionsUrl}{TwitchConstants.BroadcasterId}&reward_id={rewardId}&status={TwitchConstants.ChannelPointStatusUncompleted}";

        var message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return Array.Empty<Redemption>();

        var response = await message.Content.ReadAsStringAsync();
        var unfulfilled = JsonConvert.DeserializeObject<CpRewardRequest>(response);
        return unfulfilled?.Data ?? Array.Empty<Redemption>();
    }

    public async Task<HttpResponseMessage> UpdateRedemptionStatus(
        string id,
        string broadcasterId,
        string rewardId,
        string status)
    {
        var requestContent = new StringContent($"{{\"status\":\"{status}\"}}",
            Encoding.UTF8,
            "application/json");

        var requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{broadcasterId}&reward_id={rewardId}&id={id}";
        var message = await _TwitchHttpClient.PatchAsync(requestUrl, requestContent);
        return message;
    }
}