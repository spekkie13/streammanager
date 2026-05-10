using SpekkieClassLibrary.Twitch;

namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

public sealed record ChatCommandReceived(
    string MessageId,
    string UserId,
    string Username,
    UserRole Role,
    string? CommandText, // "song"
    string? ArgumentsAsString, // "..."
    string RawMessage
);