using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using TwitchAuthService.Handlers;

namespace CommandService.CommandHandlers;

public class TwitchCommandHandler(ChannelPointHandler channelPointHandler)
{
    public string HandleRefundCommand(string username)
    {
        if (string.IsNullOrEmpty(username)) return "";
        Redemption redemption = channelPointHandler.GetMostRecentRedemptionForUser(username).Result;

        HttpResponseMessage message = channelPointHandler.UpdateRedemptionStatus(redemption.Id, TwitchConstants.BroadcasterId,
            redemption.Reward.Id, TwitchConstants.ChannelPointStatusCancelled).Result;
        return message.IsSuccessStatusCode
            ? $"Successfully refunded most recent channel point redemption for {username}"
            : $"Unable to refund most recent channel point redemption for {username}";
    }
    
    public string HandleCompleteCommand(string username)
    {
        if (string.IsNullOrEmpty(username)) return "";
        Redemption redemption = channelPointHandler.GetMostRecentRedemptionForUser(username).Result;

        HttpResponseMessage message = channelPointHandler.UpdateRedemptionStatus(redemption.Id, TwitchConstants.BroadcasterId,
            redemption.Reward.Id, TwitchConstants.ChannelPointStatusFulfilled).Result;
        return message.IsSuccessStatusCode
            ? $"Successfully completed most recent channel point redemption for {username}"
            : $"Unable to complete most recent channel point redemption for {username}";
    }
    
    public string HandleCreateRedemptionCommand(string commandArgs)
    {
        return channelPointHandler.CreateRedemption(commandArgs);
    }
}