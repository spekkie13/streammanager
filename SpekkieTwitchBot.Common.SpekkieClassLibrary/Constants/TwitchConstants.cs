namespace SpekkieClassLibrary.Constants;

public static class TwitchConstants
{
    public static string BotExitMessage => "Bye! Have a beautiful time!";

    public static string TwitchChannelRedemptionsUrl =>
        "https://api.twitch.tv/helix/channel_points/custom_rewards/redemptions?broadcaster_id=";

    public static string TwitchChannelRewardsUrl =>
        "https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=";

    public static string TwitchFollowersUrl => "https://api.twitch.tv/helix/channels/followers";
    public static string TwitchSubscribersUrl => "https://api.twitch.tv/helix/subscriptions";
    public static string ChannelPointStatusCancelled => "CANCELED";
    public static string ChannelPointStatusFulfilled => "FULFILLED";
    public static string ChannelPointStatusUncompleted => "UNFULFILLED";
}