using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.Twitch.General;
using Reward = SpekkieClassLibrary.Twitch.Events.ChannelPoint.Reward;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class ChannelPointHandler
{
    private readonly CustomTwitchHttpClient _TwitchHttpClient;
    
    public ChannelPointHandler(CustomTwitchHttpClient client)
    {
        _TwitchHttpClient = client;
        GetAllRedemptions();
    }

    public HttpResponseMessage HandleSongRedemption(bool success, string redemptionId, string rewardId)
    {
        string status = success ? "FULFILLED" : "CANCELED";
        HttpResponseMessage message = 
            UpdateRedemptionStatus(
                           id: redemptionId, 
                broadcasterId: TwitchConstants.BroadcasterId, 
                     rewardId: rewardId, 
                       status: status).Result;

        return message;
    }
    
    private void GetAllRedemptions()
    {
        var response = GetCustomRedemptions().Result;
        if (response.Count == 0) return;
        
        List<Reward> unfulfilledRedemptions = new List<Reward>();
        foreach (string? rewardId in response)
        {
            if (string.IsNullOrEmpty(rewardId)) continue;
            List<Reward?> tempRed = GetFulfilledRedemptionsByStatus("UNFULFILLED", rewardId).Result;
            if(tempRed.Count == 0)
                continue;
            unfulfilledRedemptions.AddRange(tempRed!);
        }
        
        Console.WriteLine(unfulfilledRedemptions.Count);
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
        string rewardInfo = $"{{\"title\":\"{title}\",\"cost\":{cost},\"is_user_input_required\":{isUserInputRequired.ToString().ToLower()},\"prompt\":\"{prompt.Substring(0, Math.Min(prompt.Length, 200))}\"}}";
        var content = new StringContent(rewardInfo, Encoding.UTF8, "application/json");

        var response = _TwitchHttpClient.PostAsync(url, content).Result;

        Console.WriteLine(response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}");
    }

    private async Task<List<string?>> GetCustomRedemptions()
    {
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return new List<string?>();
        string response = await message.Content.ReadAsStringAsync();
        ChannelPointRequest redemptions = JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return new List<string?>();
        List<string?> rewardIds = redemptions.Data.Select(x => x.Id).ToList();
        return rewardIds;
    }
    
    private async Task<List<Reward?>> GetFulfilledRedemptionsByStatus(string status, string rewardId)
    {
        string url = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{TwitchConstants.BroadcasterId}&reward_id={rewardId}&status={status}";

        HttpResponseMessage message = await _TwitchHttpClient.GetAsync(url);
        if (!message.IsSuccessStatusCode) return new List<Reward?>();

        string response = await message.Content.ReadAsStringAsync();
        var unfulfilled = JsonConvert.DeserializeObject<ChannelPointRewardRequest>(response) ?? new ChannelPointRewardRequest();
        var redemptions = unfulfilled.Data;
        var rewards = redemptions?.Select(x => x.Reward).ToList() ?? new List<Reward?>();
        return rewards;
    }
    
    private async Task<HttpResponseMessage> UpdateRedemptionStatus(
        string id, 
        string broadcasterId, 
        string rewardId, 
        string status)
    {
        var requestContent = new StringContent($"{{\"status\":\"{status}\"}}", 
            Encoding.UTF8, 
            "application/json");
        
        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}?broadcaster_id={broadcasterId}&reward_id={rewardId}&id={id}";
        HttpResponseMessage message = await _TwitchHttpClient.PatchAsync(requestUrl, requestContent);
        return message;
    }
}