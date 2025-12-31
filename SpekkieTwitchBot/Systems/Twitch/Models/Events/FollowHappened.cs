namespace SpekkieTwitchBot.Systems.Twitch.Models.Events;

public sealed record FollowHappened(
    string UserId,
    string UserName,
    DateTimeOffset FollowedAt
);