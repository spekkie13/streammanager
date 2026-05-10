namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

public sealed record ChatMessageReceived(
    string MessageId,
    string UserId,
    string Username,
    string Text,
    string? CustomRewardId
);