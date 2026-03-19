using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

public record SubGoalConfig(
    [property: JsonPropertyName("goal")] int Goal,
    [property: JsonPropertyName("current")] int CurrentAmount,
    [property: JsonPropertyName("rewardEn")] string RewardEn,
    [property: JsonPropertyName("rewardNl")] string RewardNl,
    [property: JsonPropertyName("endDate")] DateOnly EndDate
);