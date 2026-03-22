using System.Text;
using Newtonsoft.Json;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;
using SpotifyAuthService;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class ChannelPointsFeature
{
    private readonly ICustomTwitchHttpClient _Client;
    private readonly ITwitchAuthTokenProvider _Tokens;
    private readonly ISpotifyService _SpotifyService;
    private readonly Logger _Logger;

    public ChannelPointsFeature(
        ICustomTwitchHttpClient client,
        ITwitchAuthTokenProvider tokens,
        ISpotifyService spotify,
        Logger logger)
    {
        _Client = client;
        _Tokens = tokens;
        _SpotifyService = spotify;
        _Logger = logger;
    }
    
    public async Task<string> OnRedeemedAsync(ChannelPointRedeemed e, CancellationToken ct)
    {
        // Guardrails
        if (string.IsNullOrWhiteSpace(e.RewardTitle))
            return "No reward title found";

        switch (e.RewardTitle)
        {
            case "Song Request":
            {
                string input = e.UserInput ?? "";
                if (string.IsNullOrWhiteSpace(input))
                    return "User input is required for this reward";

                string result = await HandleSongRedemption(input, e.RedemptionId, e.RewardId, ct);

                _Logger.LogInfo(result);
                _Logger.LogInfo($"Redeemed: {e.RewardTitle} by {e.UserName}");
                return $"@{e.UserName} {result}";
            }
            case "Hydrate":
            {
                return "Hydrate reward redeemed";
            }

            default:
                _Logger.LogInfo($"Redeemed: {e.RewardTitle} (ignored)");
                return "Ignored";
        }
    }
    
    private async Task<string> HandleSongRedemption(string input, string redemptionId, string rewardId, CancellationToken ct)
    {
        string result = await _SpotifyService.AddSongToQueueAsync(input, ct);
        bool success = !result.Equals("Error", StringComparison.OrdinalIgnoreCase);

        string status = success
            ? TwitchConstants.ChannelPointStatusFulfilled
            : TwitchConstants.ChannelPointStatusCancelled;

        await UpdateRedemptionStatus(redemptionId, rewardId, status, ct);

        return success ? $"Successfully added {result} to the queue" : $"Failed to add song to the queue";
    }
    
    public async Task<string> CreateRedemption(string commandArgs)
    {
        string[] parts = commandArgs.Split("|");
        if (parts.Length < 3)
            return "Invalid format. Expected: title|prompt|cost[|userInputRequired]";

        string title = parts[0];
        string prompt = parts[1];
        if (!int.TryParse(parts[2], out int cost))
            return "Invalid cost value.";

        bool isUserInputRequired = true;
        if (parts.Length > 3)
            isUserInputRequired = parts[^1] != "0";
        TwitchGeneralFile auth = await _Tokens.ReadIdentityAsync(CancellationToken.None);
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{auth.ChannelId}";
        string rewardInfo = JsonConvert.SerializeObject(new
        {
            title,
            cost,
            is_user_input_required = isUserInputRequired,
            prompt = prompt[..Math.Min(prompt.Length, 200)]
        });
        StringContent content = new(rewardInfo, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _Client.PostAsync(url, content).ConfigureAwait(false);
        string responseMessage = response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}";
        _Logger.LogInfo(responseMessage);
        return responseMessage;
    }

    public async Task<List<ChannelPointData>> GetCustomRedemptions(CancellationToken ct)
    {
        TwitchGeneralFile auth = await _Tokens.ReadIdentityAsync(ct);
        string? broadcasterId = auth.ChannelId;
        
        string url = $"{TwitchConstants.TwitchChannelRewardsUrl}{broadcasterId}";
        HttpResponseMessage message = await _Client.GetAsync(url, ct);
        if (!message.IsSuccessStatusCode) return [];
        
        string response = await message.Content.ReadAsStringAsync(ct);
        ChannelPointRequest redemptions =
            JsonConvert.DeserializeObject<ChannelPointRequest?>(response) ?? new ChannelPointRequest();
        if (redemptions.Data == null) return [];
        List<ChannelPointData> rewardIds = redemptions.Data.ToList();
        return rewardIds;
    }

    private async Task UpdateRedemptionStatus(
        string? id,
        string? rewardId,
        string? status,
        CancellationToken ct)
    {
        TwitchGeneralFile auth = await _Tokens.ReadIdentityAsync(ct);
        string? broadcasterId = auth.ChannelId;
        
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(rewardId) || string.IsNullOrEmpty(status))
            return;
        
        StringContent requestContent = new ($"{{\"status\":\"{status}\"}}",
            Encoding.UTF8, mediaType: "application/json");

        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}{broadcasterId}&reward_id={rewardId}&id={id}";
        await _Client.PatchAsync(requestUrl, requestContent, ct);
    }
}