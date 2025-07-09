using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using TwitchLib.PubSub.Common;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class ChannelSubscription : MessageData
{
    public ChannelSubscription(string jsonStr)
    {
        JObject jobject = JObject.Parse(jsonStr);
        Username = jobject.SelectToken("user_name")?.ToString();
        DisplayName = jobject.SelectToken("display_name")?.ToString();
        RecipientName = jobject.SelectToken("recipient_user_name")?.ToString();
        RecipientDisplayName = jobject.SelectToken("recipient_display_name")?.ToString();
        ChannelName = jobject.SelectToken("channel_name")?.ToString();
        UserId = jobject.SelectToken("user_id")?.ToString();
        RecipientId = jobject.SelectToken("recipient_id")?.ToString();
        ChannelId = jobject.SelectToken("channel_id")?.ToString();
        Time = Helpers.DateTimeStringToObject(jobject.SelectToken("time")?.ToString());
        SubscriptionPlan = jobject.SelectToken("sub_plan")?.ToString().ToLower() switch
        {
            "prime" => SubscriptionPlan.Prime,
            "1000" => SubscriptionPlan.Tier1,
            "2000" => SubscriptionPlan.Tier2,
            "3000" => SubscriptionPlan.Tier3,
            _ => throw new ArgumentOutOfRangeException(SubscriptionPlan.ToString())
        };

        SubscriptionPlanName = jobject.SelectToken("sub_plan_name")?.ToString();
        SubMessage = new SubMessage(jobject.SelectToken("sub_message") ?? "");
        string? str1 = jobject.SelectToken("is_gift")?.ToString();
        if (str1 != null) 
            IsGift = Convert.ToBoolean(str1);
        string? str2 = jobject.SelectToken("multi_month_duration")?.ToString();
        if (str2 != null)
            MultiMonthDuration = int.Parse(str2);
        Context = jobject.SelectToken("context")?.ToString();
        JToken? jtoken1 = jobject.SelectToken("months");
        if (jtoken1 != null)
            Months = int.Parse(jtoken1.ToString());
        JToken? jtoken2 = jobject.SelectToken("cumulative_months");
        if (jtoken2 != null)
            CumulativeMonths = int.Parse(jtoken2.ToString());
        JToken? jtoken3 = jobject.SelectToken("streak_months");
        if (jtoken3 == null)
            return;
        StreakMonths = int.Parse(jtoken3.ToString());
    }

    public string? Username { get; }
    public string? DisplayName { get; }
    public string? RecipientName { get; }
    public string? RecipientDisplayName { get; }
    public string? ChannelName { get; }
    public string? UserId { get; }
    public string? ChannelId { get; }
    public string? RecipientId { get; }
    public DateTime Time { get; }
    private SubscriptionPlan SubscriptionPlan { get; }
    public string? SubscriptionPlanName { get; }
    public int Months { get; }
    public int CumulativeMonths { get; }
    public int StreakMonths { get; }
    public string? Context { get; }
    public SubMessage? SubMessage { get; }
    public bool IsGift { get; }
    public int MultiMonthDuration { get; }
    
    public static ChannelSubscription Empty => new ("");
}