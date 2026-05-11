namespace SpekkieTwitchBot.Systems.Twitch.Models.Events;

public sealed record DonationHappened(
    string UserName,
    decimal Amount,
    string Currency,
    string? Message,
    DateTimeOffset Timestamp
);
