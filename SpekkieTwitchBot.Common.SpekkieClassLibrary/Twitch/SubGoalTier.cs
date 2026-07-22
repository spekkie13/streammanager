using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

// One milestone in the sub-goal ladder: a target sub count and the reward unlocked at that count.
public record SubGoalTier(
    [property: JsonPropertyName("goal")] int Goal,
    [property: JsonPropertyName("rewardEn")] string RewardEn,
    [property: JsonPropertyName("rewardNl")] string RewardNl
);
