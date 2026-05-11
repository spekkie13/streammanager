namespace SpekkieTwitchBot.Systems.Twitch.Models.Events;

public sealed record BitsHappened(
    string UserId,
    string UserName,
    bool IsAnonymous,
    int Bits,
    string? Message,
    DateTimeOffset Timestamp
);
