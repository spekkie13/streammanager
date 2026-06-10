namespace SpekkieTwitchBot.Systems.Twitch.Models.BaseReview;

/// <summary>A single queued base-review request created from a "Base Review" channel-point redemption.</summary>
public sealed record BaseReviewEntry(
    string? UserId,
    string UserName,
    string Input,
    bool IsSubscriber,
    string? Tier,
    DateTimeOffset RedeemedAt,
    string RedemptionId,
    string RewardId
);
