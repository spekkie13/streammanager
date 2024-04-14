using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.Models.Twitch.Auth;
using SpekkieTwitchBot.Web;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class ChannelPointHandler
{
    private readonly TwitchUserAuth _TwitchUserAuth;
    private readonly CustomTwitchHttpClient _TwitchHttpClient;
    
    public ChannelPointHandler(
        TwitchUserAuth twitchUserAuth,
        CustomTwitchHttpClient client)
    {
        _TwitchUserAuth = twitchUserAuth;
        _TwitchHttpClient = client;
    }
    
    public void CreateRedemption(string commandArgs)
    {
        string title = commandArgs.Split("|")[0];
        string prompt = commandArgs.Split("|")[1];
        int cost = Convert.ToInt32(commandArgs.Split("|")[2]);
        bool isUserInputRequired = true;

        if (commandArgs.Split("|").Length > 3)
            isUserInputRequired = Convert.ToInt32(commandArgs.Split("|").Last()) != 0;
        const string Url = "https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=30731359";
        string rewardInfo = $"{{\"title\":\"{title}\",\"cost\":{cost},\"is_user_input_required\":{isUserInputRequired.ToString().ToLower()},\"prompt\":\"{prompt.Substring(0, Math.Min(prompt.Length, 200))}\"}}";
        var content = new StringContent(rewardInfo, Encoding.UTF8, "application/json");

        var response = _TwitchHttpClient.PostAsync(Url, content).Result;

        Console.WriteLine(response.IsSuccessStatusCode
            ? "Custom reward created successfully!"
            : $"Failed to create custom reward. Status code: {response.StatusCode}");
    }

    public async Task<List<Redemption>?> GetCustomRedemptions()
    {
        string url = TwitchConstants.TwitchChannelRedemptionsUrl;

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("client-id", _TwitchUserAuth.ClientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _TwitchUserAuth.UserToken);

        HttpResponseMessage message = await client.GetAsync(url);
        if (!message.IsSuccessStatusCode) return null;
        string response = await message.Content.ReadAsStringAsync();
        List<Redemption>? redemptions = JsonConvert.DeserializeObject<List<Redemption>?>(response);
        return redemptions;

    }
    
    public async Task<HttpResponseMessage> UpdateRedemptionStatus(string id, string broadcasterId, string rewardId, string status)
    {
        var requestContent = new StringContent($"{{\"status\":\"{status}\"}}", 
            Encoding.UTF8, 
            "application/json");
        
        string requestUrl = $"{TwitchConstants.TwitchChannelRedemptionsUrl}?broadcaster_id={broadcasterId}&reward_id={rewardId}&id={id}";
        HttpResponseMessage message = await _TwitchHttpClient.PatchAsync(requestUrl, requestContent);
        return message;
    }
}