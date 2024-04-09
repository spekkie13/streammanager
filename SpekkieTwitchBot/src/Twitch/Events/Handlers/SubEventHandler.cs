using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages;

namespace SpekkieTwitchBot.Twitch.Events.Handlers;

public class SubEventHandler
{
    public void HandleSub(object? sender, OnChannelSubscriptionArgs e)
    {
        ChannelSubscription subscription = e.Subscription;
        if(!string.IsNullOrEmpty(subscription.RecipientName))
            HandleGiftedSub(e.Subscription);
        else
            HandleSelfSub(e.Subscription);
    }

    private void HandleSelfSub(ChannelSubscription subscription)
    {
        /* sub name => sub file
         * sub counter++
         * 
         */
    }

    private void HandleGiftedSub(ChannelSubscription subscription)
    {
        /* sub name => sub file
         * sub gifter name => gifter file
         * sub counter ++
         * 
         */
    }
}