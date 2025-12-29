namespace SpekkieTwitchBot.Systems.Twitch.Models;

public sealed record SubHappened(
    SubKind Kind,
    string RecipientUserId,
    string RecipientUserName,
    string? GifterUserId,
    string? GifterUserName,
    
    string Tier,
    bool IsPrime,
    int? TotalMonths,
    int GiftCount,
    
    string? Message,
    DateTimeOffset Timestamp
);

public enum SubKind
{
    New,
    Resub,
    Gift,
    CommunityGift,
    PrimePaidUpgrade,
    ContinuedGift
}