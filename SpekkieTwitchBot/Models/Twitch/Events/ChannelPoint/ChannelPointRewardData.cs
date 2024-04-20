namespace SpekkieTwitchBot.Models.Twitch.Events.ChannelPoint;

public class ChannelPointRewardData
{
    public string broadcaster_name { get; set; }
    public string broadcaster_login { get; set; }
    public string broadcaster_id { get; set; }
    public string id { get; set; }
    public string user_id { get; set; }
    public string user_name { get; set; }
    public string user_input { get; set; }
    public string status { get; set; }
    public string redeemed_at { get; set; }
    public Reward reward { get; set; }
}