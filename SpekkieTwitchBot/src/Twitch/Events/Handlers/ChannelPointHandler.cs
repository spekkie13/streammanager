using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Twitch.General;
using Redemption = SpekkieClassLibrary.Twitch.Events.ChannelPoint.Redemption;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class ChannelPointHandler
{
    private readonly CustomTwitchHttpClient _TwitchHttpClient;
    private readonly Logger _Logger;

    public ChannelPointHandler(CustomTwitchHttpClient client, Logger logger)
    {
        _TwitchHttpClient = client;
        _Logger = logger;
    }

    public HttpResponseMessage HandleSongRedemption(bool success, string redemptionId, string rewardId)
    {
        string status = success ? TwitchConstants.ChannelPointStatusFulfilled : TwitchConstants.ChannelPointStatusCancelled;
        HttpResponseMessage message =
            UpdateRedemptionStatus(
                id: redemptionId,
                broadcasterId: TwitchConstants.BroadcasterId,
                rewardId: rewardId,
                status: status).Result;

        return message;
    }

    public async Task<Redemption> GetMostRecentRedemptionForUser(string username)
    {
        List<string> redemptionIds = await GetCustomRedemptions();
        List<Redemption> outstandingRedemptions = new List<Redemption>();
        foreach (string id in redemptionIds)
        {
            Redemption[] redemptionData = await GetRedemptionsByStatus(id);
            outstandingRedemptions.AddRange(redemptionData.ToList());
        }

        outstandingRedemptions.Sort((r1, r2) => DateTime.Compare(DateTime.Parse(r1.RedeemedAt), DateTime.Parse(r2.RedeemedAt)));
        Redemption red = outstandingRedemptions.First(red => red.UserName == username);
        return red;
    }

    public void CreateRedemption(string commandArgs)
    {
        string title = commandArgs.Split("|")[0];
        string prompt = commandArgs.Split("|")[1];
        int cost = Convert.ToInt32(commandArgs.Split("|")[2]);
        bool isUserInputRequired = true;

        if (commandArgs.Split("|").Length > 3)
            isUserInputRequired = Convert.ToInt32(commandArgs.Split("|").Last()) != 0;
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}30731359";
        string rewardInfo =
            $"{{\"title\":\"{title}\",\"cost\":{cost},\"is_user_input_required\":{isUserInputRequired.ToString().ToLower()},\"prompt\":\"{prompt.Substring(0, Math.Min(prompt.Length, 200))}\"}}";
        StringContent content = new StringContent(rewardInfo, Encoding.UTF8, "application/json");

        HttpResponseMessage response = _TwitchHttpClient.PostAsync(url, content).Result;

        _Logger.LogInfo(response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}");
    }

    private async Task<List<string>> GetCustomRedemptions()
    {
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return new List<string>();
        string response = await message.Content.ReadAsStringAsync();
        ChannelPointRequest redemptions =
            JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return new List<string>();
        List<string> rewardIds = redemptions.Data.Select(x => x.Id).ToList();
        return rewardIds;
    }

    private async Task<Redemption[]> GetRedemptionsByStatus(string rewardId)
    {
        string url = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{TwitchConstants.BroadcasterId}&reward_id={rewardId}&status={TwitchConstants.ChannelPointStatusUncompleted}";

        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return Array.Empty<Redemption>();

        string response = await message.Content.ReadAsStringAsync();
        CpRewardRequest? unfulfilled = JsonConvert.DeserializeObject<CpRewardRequest>(response);
        return unfulfilled?.Data ?? Array.Empty<Redemption>();
    }

    public async Task<HttpResponseMessage> UpdateRedemptionStatus(
        string id,
        string broadcasterId,
        string rewardId,
        string status)
    {
        StringContent requestContent = new StringContent($"{{\"status\":\"{status}\"}}",
            Encoding.UTF8,
            "application/json");

        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{broadcasterId}&reward_id={rewardId}&id={id}";
        HttpResponseMessage message = await _TwitchHttpClient.PatchAsync(requestUrl, requestContent);
        return message;
    }
}