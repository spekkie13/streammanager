using SpekkieClassLibrary.Constants;
using TwitchAuthService.Handlers;

namespace CommandService.CommandHandlers;

public class TwitchCommandHandler(ChannelPointHandler channelPointHandler, IrcClient ircClient)
{
    public void HandleRefundCommand(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        var redemption = channelPointHandler.GetMostRecentRedemptionForUser(username).Result;

        var message = channelPointHandler.UpdateRedemptionStatus(redemption.Id, TwitchConstants.BroadcasterId,
            redemption.Reward.Id, TwitchConstants.ChannelPointStatusCancelled).Result;
        ircClient.SendPublicChatMessage(message.IsSuccessStatusCode
            ? $"Successfully refunded most recent channel point redemption for {username}"
            : $"Unable to refund most recent channel point redemption for {username}");
    }
    
    public void HandleCompleteCommand(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        var redemption = channelPointHandler.GetMostRecentRedemptionForUser(username).Result;

        var message = channelPointHandler.UpdateRedemptionStatus(redemption.Id, TwitchConstants.BroadcasterId,
            redemption.Reward.Id, TwitchConstants.ChannelPointStatusFulfilled).Result;
        ircClient.SendPublicChatMessage(message.IsSuccessStatusCode
            ? $"Successfully completed most recent channel point redemption for {username}"
            : $"Unable to complete most recent channel point redemption for {username}");
    }
    
    public void HandleCreateRedemptionCommand(string commandArgs)
    {
        channelPointHandler.CreateRedemption(commandArgs);
    }
}