using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Features;

public class FollowSubFeature
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchFileWriter _Files;
    private readonly CustomTwitchHttpClient _Api;
    
    public FollowSubFeature(
        ITwitchChat chat,
        CustomTwitchHttpClient api,
        ITwitchFileWriter files
    ) {
        _Chat = chat;
        _Api = api;
        _Files = files;
    }

    public async Task HandleFollowAsync(FollowHappened e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(e.UserName)) return;
        
        await _Files.WriteMostRecentFollowerAsync(e.UserName, cancellationToken);
        
        var totalFollowers = await _Api.GetFollowerCount(cancellationToken);
        await _Files.WriteTotalFollowersAsync(totalFollowers, cancellationToken);
        
        await _Chat.SendAsync(message: $"Thanks for the follow {e.UserName}", cancellationToken);
    }

    public async Task HandleSubAsync(SubHappened e, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(e.RecipientUserName)) return;

        var latestSubscriber = FormatLatestSub(e);
        await _Files.WriteMostRecentSubscriberAsync(latestSubscriber, cancellationToken);
        
        var totalSubs = await _Api.GetSubscriberCount(cancellationToken);
        await _Files.WriteTotalSubscribersAsync(totalSubs, cancellationToken);
        
        await _Chat.SendAsync(message: FormatChatThanks(e), cancellationToken);
    }

    private static string FormatLatestSub(SubHappened e)
    {
        return e.Kind switch
        {
            SubKind.New => $"{e.RecipientUserName} subscribed (Tier {HumanTier(e.Tier)})",
            SubKind.Resub => $"{e.RecipientUserName} resubbed ({e.TotalMonths ?? 0} months, Tier {HumanTier(e.Tier)})",
            SubKind.Gift => $"{e.GifterUserName ?? "Someone"} gifted a sub to {e.RecipientUserName} (Tier {HumanTier(e.Tier)})",
            SubKind.CommunityGift => $"{e.GifterUserName ?? "Someone"} gifted subs to the community",
            SubKind.PrimePaidUpgrade => $"{e.RecipientUserName} upgraded from Prime",
            SubKind.ContinuedGift => $"{e.RecipientUserName} continued a gifted sub",
            _ => $"{e.RecipientUserName} subscribed"
        };
    }

    private static string HumanTier(string tier) =>
        tier switch
        {
            "prime" => "Prime",
            "1000" => "1",
            "2000" => "2",
            "3000" => "3",
            _ => tier
        };
    
    private static string FormatChatThanks(SubHappened e)
    {
        return e.Kind switch
        {
            SubKind.New => $"Welcome {e.RecipientUserName}! Thanks for subscribing ❤️",
            SubKind.Resub => $"Welcome back {e.RecipientUserName}! ❤️ ({e.TotalMonths ?? 0} months!)",
            SubKind.Gift => $"Huge thanks {e.GifterUserName ?? "friend"} for gifting a sub to {e.RecipientUserName}! 🎁",
            SubKind.CommunityGift => $"Insane! Thanks {e.GifterUserName ?? "friend"} for the community gift! 🎁",
            SubKind.PrimePaidUpgrade => $"Thanks for upgrading, {e.RecipientUserName}! ❤️",
            SubKind.ContinuedGift => $"Love it {e.RecipientUserName} — thanks for continuing the gifted sub! ❤️",
            _ => $"Thanks for subscribing, {e.RecipientUserName}! ❤️"
        };
    }
}