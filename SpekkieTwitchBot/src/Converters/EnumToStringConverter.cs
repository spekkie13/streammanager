using TwitchLib.PubSub.Enums;

namespace SpekkieTwitchBot.Converters;

public static class EnumToStringConverter
{
    public static string AutomodQueueTypeToString(AutomodQueueType type)
    {
        return type.ToString();
    }

    public static string ChannelPointsChannelTypeToString(ChannelPointsChannelType type)
    {
        return type.ToString();
    }
    
    public static string CommunityPointsChannelTypeToString(CommunityPointsChannelType type)
    {
        return type.ToString();
    }

    public static string LeaderboardTypeToString(LeaderBoardType type)
    {
        return type.ToString();
    }
}