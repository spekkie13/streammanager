using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class ChannelPointsFeature
{
    private readonly CustomTwitchHttpClient _Client;
    private readonly SpotifyCommandHandler _Spotify;
    private readonly ITwitchChat _Chat;
    private readonly Logger _Logger;

    public ChannelPointsFeature(
        CustomTwitchHttpClient client,
        SpotifyCommandHandler spotify,
        ITwitchChat chat,
        Logger logger)
    {
        _Client = client;
        _Spotify = spotify;
        _Chat = chat;
        _Logger = logger;
    }
    
    public async Task OnRedeemedAsync(ChannelPointRedeemed e, CancellationToken ct)
    {
        // Guardrails
        if (string.IsNullOrWhiteSpace(e.RewardTitle))
            return;

        switch (e.RewardTitle)
        {
            case "Song Request":
            {
                string input = e.UserInput ?? "";
                if (string.IsNullOrWhiteSpace(input))
                    return;
                
                string result = await HandleSongRedemption(input, e.RedemptionId, e.RewardId, ct);

                _Logger.LogInfo(result);

                await _Chat.SendAsync(result, ct);

                _Logger.LogInfo($"Redeemed: {e.RewardTitle} by {e.UserName}");
                break;
            }

            default:
                _Logger.LogInfo($"Redeemed: {e.RewardTitle} (ignored)");
                break;
        }
    }
    
    private async Task<string> HandleSongRedemption(string input, string redemptionId, string rewardId, CancellationToken ct)
    {
        string result = _Spotify.HandleAddSongToQueueCommand(input);
        bool success = result.Contains("Added", StringComparison.OrdinalIgnoreCase);
        
        string status = success
            ? TwitchConstants.ChannelPointStatusFulfilled
            : TwitchConstants.ChannelPointStatusCancelled;
        
        await UpdateRedemptionStatus(redemptionId, TwitchConstants.BroadcasterId, rewardId, status, ct);
        
        return success ? $"successfully added {input} to queue" : $"failed to add {input} to queue";
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

        HttpResponseMessage response = _Client.PostAsync(url, content).Result;
        string responseMessage = response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}";
        _Logger.LogInfo(responseMessage);
        return responseMessage;
    }

    public async Task<List<ChannelPointData>> GetCustomRedemptions()
    {
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{TwitchConstants.BroadcasterId}";
        HttpResponseMessage message = await _Client.GetAsync(url);
        if (!message.IsSuccessStatusCode) return [];
        
        string response = await message.Content.ReadAsStringAsync();
        ChannelPointRequest redemptions =
            JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return [];
        List<ChannelPointData> rewardIds = redemptions.Data.ToList();
        return rewardIds;
    }

    private async Task UpdateRedemptionStatus(
        string? id,
        string? broadcasterId,
        string? rewardId,
        string? status,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(rewardId) || string.IsNullOrEmpty(status))
            return;
        
        StringContent requestContent = new ($"{{\"status\":\"{status}\"}}",
            Encoding.UTF8, mediaType: "application/json");

        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{broadcasterId}&reward_id={rewardId}&id={id}";
        await _Client.PatchAsync(requestUrl, requestContent, ct);
    }
}