using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

public record SubGoalConfig(
    [property: JsonPropertyName("goal")] int Goal,
    [property: JsonPropertyName("current")] int CurrentAmount,
    [property: JsonPropertyName("endDate")] DateOnly EndDate
);