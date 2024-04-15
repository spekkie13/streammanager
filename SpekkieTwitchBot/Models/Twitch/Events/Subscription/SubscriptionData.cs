namespace SpekkieTwitchBot.Models.Twitch.Events.Subscription;

public class SubscriptionData
{
    public string broadcaster_id { get; set; }
    public string broadcaster_login { get; set; }
    public string broadcaster_name { get; set; }
    public string gifter_id { get; set; }
    public string gifter_login { get; set; }
    public string gifter_name { get; set; }
    public bool isGift { get; set; }
    public string tier { get; set; }
    public string plan_name { get; set; }
    public string user_id { get; set; }
    public string user_name { get; set; }
    public string user_login { get; set; }
}