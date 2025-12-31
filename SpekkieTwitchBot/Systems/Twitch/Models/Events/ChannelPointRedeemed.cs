namespace SpekkieTwitchBot.Systems.Twitch.Models.Events;

public sealed record ChannelPointRedeemed(
    string RedemptionId,
    string RewardId,
    string RewardTitle,
    string UserId,
    string UserName,
    string? UserInput,
    DateTimeOffset RedeemedAt
);