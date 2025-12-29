namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

public sealed record ChatCommandReceived(
    string MessageId,
    string UserId,
    string Username,
    string? CommandText, // "song"
    string? ArgumentsAsString, // "..."
    string RawMessage
);