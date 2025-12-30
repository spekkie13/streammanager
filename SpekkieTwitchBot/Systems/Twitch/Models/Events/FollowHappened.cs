namespace SpekkieTwitchBot.Systems.Twitch.Models;

public sealed record FollowHappened(
    string UserId,
    string UserName,
    DateTimeOffset FollowedAt
);